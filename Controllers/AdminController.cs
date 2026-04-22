using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;
using System.Globalization;

[AdminAuthorize]
public class AdminController : Controller
{
    private readonly MobileRechargeDbContext _context;

    public AdminController(MobileRechargeDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult TrendData(string period = "week", DateTime? from = null, DateTime? to = null)
    {
        var normalizedPeriod = (period ?? string.Empty).Trim().ToLowerInvariant();
        var now = DateTime.Now;

        var normalizedTo = (to ?? now).Date;
        var normalizedFrom = (from ?? normalizedTo.AddDays(-83)).Date;

        if (normalizedFrom > normalizedTo)
        {
            return BadRequest("Invalid date range. 'from' must be <= 'to'.");
        }

        switch (normalizedPeriod)
        {
            case "week":
                return Json(BuildWeeklyTrends(normalizedFrom, normalizedTo));
            case "month":
                return Json(BuildMonthlyTrends(normalizedFrom, normalizedTo));
            case "year":
                return Json(BuildYearlyTrends(normalizedFrom, normalizedTo));
            default:
                return BadRequest("Invalid period. Use week, month, or year.");
        }
    }

    [HttpGet]
    public IActionResult RevenueData(string period = "week", DateTime? from = null, DateTime? to = null)
    {
        var normalizedPeriod = (period ?? string.Empty).Trim().ToLowerInvariant();
        var now = DateTime.Now;

        var normalizedTo = (to ?? now).Date;
        var normalizedFrom = (from ?? normalizedTo.AddDays(-83)).Date;

        if (normalizedFrom > normalizedTo)
        {
            return BadRequest("Invalid date range. 'from' must be <= 'to'.");
        }

        switch (normalizedPeriod)
        {
            case "week":
                return Json(BuildWeeklyRevenue(normalizedFrom, normalizedTo));
            case "month":
                return Json(BuildMonthlyRevenue(normalizedFrom, normalizedTo));
            case "year":
                return Json(BuildYearlyRevenue(normalizedFrom, normalizedTo));
            default:
                return BadRequest("Invalid period. Use week, month, or year.");
        }
    }

    public IActionResult Dashboard()
    {
        ViewBag.TotalUsers = _context.Users.Count(x => !x.IsDeleted && x.Role == "User");
        ViewBag.UnpaidPostpaidBills = _context.Transactions.Count(x =>
            x.Type == TransactionType.Postpaid &&
            x.Status == TransactionStatus.Pending &&
            !x.IsPaid);
        ViewBag.RecentTransactions = _context.Transactions
            .Include(x => x.User)
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToList();
        ViewBag.RecentFeedbacks = _context.Feedbacks
            .Include(x => x.User)
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToList();

        return View();
    }

    private object BuildWeeklyRevenue(DateTime from, DateTime to)
    {
        var start = GetWeekStart(from);
        var endBucketStart = GetWeekStart(to);
        var weeks = (int)((endBucketStart - start).TotalDays / 7) + 1;

        if (weeks > 120)
        {
            return new
            {
                period = "week",
                error = "Date range too large for weekly grouping (max 120 weeks)."
            };
        }

        var endExclusive = endBucketStart.AddDays(7);

        var labels = Enumerable.Range(0, weeks)
            .Select(i => start.AddDays(7 * i).ToString("dd/MM", CultureInfo.InvariantCulture))
            .ToArray();

        var paidTransactions = _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.Status == TransactionStatus.Success &&
                t.IsPaid &&
                t.CreatedAt >= start &&
                t.CreatedAt < endExclusive)
            .Select(t => new { t.CreatedAt, t.Amount })
            .ToList();

        var revenue = new decimal[weeks];
        decimal totalRevenue = 0m;

        foreach (var item in paidTransactions)
        {
            var bucketStart = GetWeekStart(item.CreatedAt);
            var index = (int)((bucketStart - start).TotalDays / 7);
            if (index >= 0 && index < weeks)
            {
                revenue[index] += item.Amount;
                totalRevenue += item.Amount;
            }
        }

        return new
        {
            period = "week",
            labels,
            revenue,
            totalRevenue
        };
    }

    private object BuildMonthlyRevenue(DateTime from, DateTime to)
    {
        var start = new DateTime(from.Year, from.Month, 1);
        var endBucketStart = new DateTime(to.Year, to.Month, 1);
        var months = (endBucketStart.Year - start.Year) * 12 + (endBucketStart.Month - start.Month) + 1;

        if (months > 120)
        {
            return new
            {
                period = "month",
                error = "Date range too large for monthly grouping (max 120 months)."
            };
        }

        var endExclusive = endBucketStart.AddMonths(1);

        var labels = Enumerable.Range(0, months)
            .Select(i => start.AddMonths(i).ToString("MM/yyyy", CultureInfo.InvariantCulture))
            .ToArray();

        var paidTransactions = _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.Status == TransactionStatus.Success &&
                t.IsPaid &&
                t.CreatedAt >= start &&
                t.CreatedAt < endExclusive)
            .Select(t => new { t.CreatedAt, t.Amount })
            .ToList();

        var revenue = new decimal[months];
        decimal totalRevenue = 0m;

        foreach (var item in paidTransactions)
        {
            var bucketStart = new DateTime(item.CreatedAt.Year, item.CreatedAt.Month, 1);
            var index = (bucketStart.Year - start.Year) * 12 + (bucketStart.Month - start.Month);
            if (index >= 0 && index < months)
            {
                revenue[index] += item.Amount;
                totalRevenue += item.Amount;
            }
        }

        return new
        {
            period = "month",
            labels,
            revenue,
            totalRevenue
        };
    }

    private object BuildYearlyRevenue(DateTime from, DateTime to)
    {
        var startYear = from.Year;
        var endYear = to.Year;
        var years = endYear - startYear + 1;

        if (years > 50)
        {
            return new
            {
                period = "year",
                error = "Date range too large for yearly grouping (max 50 years)."
            };
        }

        var start = new DateTime(startYear, 1, 1);
        var endExclusive = new DateTime(endYear + 1, 1, 1);

        var labels = Enumerable.Range(0, years)
            .Select(i => (startYear + i).ToString(CultureInfo.InvariantCulture))
            .ToArray();

        var paidTransactions = _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.Status == TransactionStatus.Success &&
                t.IsPaid &&
                t.CreatedAt >= start &&
                t.CreatedAt < endExclusive)
            .Select(t => new { t.CreatedAt, t.Amount })
            .ToList();

        var revenue = new decimal[years];
        decimal totalRevenue = 0m;

        foreach (var item in paidTransactions)
        {
            var index = item.CreatedAt.Year - startYear;
            if (index >= 0 && index < years)
            {
                revenue[index] += item.Amount;
                totalRevenue += item.Amount;
            }
        }

        return new
        {
            period = "year",
            labels,
            revenue,
            totalRevenue
        };
    }

    public IActionResult Users()
    {
        return View(_context.Users
            .Where(u => !u.IsDeleted)
            .ToList());
    }

    public IActionResult Products()
    {
        var now = DateTime.Now;
        var products = _context.Products
            .AsNoTracking()
            .OrderByDescending(x => x.IsTop)
            .ThenByDescending(x => x.IsSpecial)
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Price)
            .ToList();

        var productIds = products.Select(x => x.Id).ToList();
        var activeSales = _context.ProductSales
            .AsNoTracking()
            .Where(x =>
                productIds.Contains(x.ProductId) &&
                x.SaleType != ProductSaleType.None &&
                x.SaleValue > 0 &&
                (x.StartAt == null || x.StartAt <= now) &&
                (x.EndAt == null || x.EndAt >= now))
            .OrderByDescending(x => x.StartAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id)
            .ToList()
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First());

        ViewBag.PricingByProductId = products.ToDictionary(
            x => x.Id,
            x =>
            {
                activeSales.TryGetValue(x.Id, out var sale);
                return new Online_Mobile_Recharge.Models.ViewModels.ProductPricingInfoViewModel
                {
                    ProductId = x.Id,
                    OriginalPrice = x.Price,
                    EffectivePrice = Online_Mobile_Recharge.Services.ProductSaleCalculator.GetEffectivePrice(x.Price, sale),
                    HasSale = Online_Mobile_Recharge.Services.ProductSaleCalculator.IsActive(sale, now),
                    SaleBadgeText = Online_Mobile_Recharge.Services.ProductSaleCalculator.GetBadgeText(sale)
                };
            });

        return View(products);
    }

    public IActionResult EditProduct(int id)
    {
        var product = _context.Products.FirstOrDefault(x => x.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(Product model)
    {
        var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (product == null)
        {
            return NotFound();
        }

        var now = DateTime.Now;
        var hasActiveSubscriptions = _context.PhoneDataSubscriptions
            .AsNoTracking()
            .Any(x => x.ProductId == product.Id && x.ExpiresAt > now);

        if (hasActiveSubscriptions)
        {
            var newProduct = new Product
            {
                Name = (model.Name ?? string.Empty).Trim(),
                Description = (model.Description ?? string.Empty).Trim(),
                Price = model.Price,
                Type = model.Type,
                ValidDays = model.ValidDays,
                IsTop = model.IsTop,
                IsSpecial = model.IsSpecial
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "This product is currently in use. A new product version was created so existing users can keep their current package until expiry.";
            return RedirectToAction(nameof(Products));
        }

        product.Name = (model.Name ?? string.Empty).Trim();
        product.Description = (model.Description ?? string.Empty).Trim();
        product.Price = model.Price;
        product.Type = model.Type;
        product.ValidDays = model.ValidDays;
        product.IsTop = model.IsTop;
        product.IsSpecial = model.IsSpecial;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Product updated successfully.";
        return RedirectToAction(nameof(Products));
    }

    public IActionResult Sales(string filter = "active", int? productId = null)
    {
        var now = DateTime.Now;
        var normalizedFilter = (filter ?? "active").Trim().ToLowerInvariant();

        var productSalesQuery = _context.ProductSales
            .Include(x => x.Product)
            .AsNoTracking()
            .AsQueryable();

        if (productId.HasValue)
        {
            productSalesQuery = productSalesQuery.Where(x => x.ProductId == productId.Value);
        }

        productSalesQuery = normalizedFilter switch
        {
            "upcoming" => productSalesQuery.Where(x => x.StartAt != null && x.StartAt > now),
            "expired" => productSalesQuery.Where(x => x.EndAt != null && x.EndAt < now),
            "all" => productSalesQuery,
            _ => productSalesQuery.Where(x =>
                x.SaleType != ProductSaleType.None &&
                x.SaleValue > 0 &&
                (x.StartAt == null || x.StartAt <= now) &&
                (x.EndAt == null || x.EndAt >= now))
        };

        var productSales = productSalesQuery
            .OrderByDescending(x => x.StartAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id)
            .ToList();

        var salesQuery = _context.Sales
            .AsNoTracking()
            .AsQueryable();

        salesQuery = normalizedFilter switch
        {
            "upcoming" => salesQuery.Where(x => x.StartAt != null && x.StartAt > now),
            "expired" => salesQuery.Where(x => x.EndAt != null && x.EndAt < now),
            "all" => salesQuery,
            _ => salesQuery.Where(x =>
                x.IsEnabled &&
                x.SaleType != ProductSaleType.None &&
                x.SaleValue > 0 &&
                (x.StartAt == null || x.StartAt <= now) &&
                (x.EndAt == null || x.EndAt >= now))
        };

        var sales = salesQuery
            .OrderByDescending(x => x.StartAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id)
            .ToList();

        return View(new AdminSalesPageViewModel
        {
            Sales = sales,
            ProductSales = productSales,
            Filter = normalizedFilter,
            ProductId = productId
        });
    }

    [HttpGet]
    public IActionResult CreateSale(int? productId = null)
    {
        ViewBag.Products = new SelectList(
            _context.Products.AsNoTracking().OrderBy(x => x.Name).ToList(),
            "Id",
            "Name",
            productId);

        return View(new ProductSale
        {
            ProductId = productId ?? 0,
            SaleType = ProductSaleType.Percent,
            SaleValue = 10,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(7)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSale(ProductSale model)
    {
        ValidateSaleModel(model);

        if (!ModelState.IsValid)
        {
            ViewBag.Products = new SelectList(
                _context.Products.AsNoTracking().OrderBy(x => x.Name).ToList(),
                "Id",
                "Name",
                model.ProductId);
            return View(model);
        }

        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = null;

        _context.ProductSales.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Sale created successfully.";
        return RedirectToAction(nameof(Sales), new { filter = "active" });
    }

    [HttpGet]
    public IActionResult EditSale(int id)
    {
        var sale = _context.ProductSales.FirstOrDefault(x => x.Id == id);
        if (sale == null)
        {
            return NotFound();
        }

        ViewBag.Products = new SelectList(
            _context.Products.AsNoTracking().OrderBy(x => x.Name).ToList(),
            "Id",
            "Name",
            sale.ProductId);

        return View(sale);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSale(ProductSale model)
    {
        var sale = await _context.ProductSales.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (sale == null)
        {
            return NotFound();
        }

        ValidateSaleModel(model);
        if (!ModelState.IsValid)
        {
            ViewBag.Products = new SelectList(
                _context.Products.AsNoTracking().OrderBy(x => x.Name).ToList(),
                "Id",
                "Name",
                model.ProductId);
            return View(model);
        }

        sale.ProductId = model.ProductId;
        sale.SaleType = model.SaleType;
        sale.SaleValue = model.SaleValue;
        sale.StartAt = model.StartAt;
        sale.EndAt = model.EndAt;
        sale.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Sale updated successfully.";
        return RedirectToAction(nameof(Sales), new { filter = "active" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSale(int id)
    {
        var sale = await _context.ProductSales.FirstOrDefaultAsync(x => x.Id == id);
        if (sale == null)
        {
            return NotFound();
        }

        _context.ProductSales.Remove(sale);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Sale deleted successfully.";
        return RedirectToAction(nameof(Sales), new { filter = "active" });
    }

    private void ValidateSaleModel(ProductSale model)
    {
        if (model.ProductId <= 0)
        {
            ModelState.AddModelError(nameof(model.ProductId), "Please choose a product.");
        }

        if (model.SaleType == ProductSaleType.None)
        {
            ModelState.AddModelError(nameof(model.SaleType), "Sale type cannot be None.");
        }

        if (model.SaleValue <= 0)
        {
            ModelState.AddModelError(nameof(model.SaleValue), "Sale value must be greater than 0.");
        }

        if (model.StartAt is DateTime start && model.EndAt is DateTime end && start > end)
        {
            ModelState.AddModelError(nameof(model.EndAt), "End time must be after start time.");
        }

        if (model.SaleType == ProductSaleType.Percent && (model.SaleValue <= 0 || model.SaleValue > 100))
        {
            ModelState.AddModelError(nameof(model.SaleValue), "Percent sale must be between 1 and 100.");
        }
    }

    public IActionResult EditUser(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        user.Password = string.Empty;
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(User model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == model.Id);

        if (user == null)
        {
            return NotFound();
        }

        if (user.Role == "Admin")
        {
            return BadRequest("Cannot modify admin");
        }

        var normalizedEmail = PasswordHelper.NormalizeEmail(model.Email);
        var emailInUse = await _context.Users.AnyAsync(u => u.Id != model.Id && u.Email == normalizedEmail);
        if (emailInUse)
        {
            ViewBag.Error = "This email is already used by another account.";
            model.Password = string.Empty;
            return View(model);
        }

        user.Name = (model.Name ?? string.Empty).Trim();
        user.Email = normalizedEmail;
        user.PhoneNumber = (model.PhoneNumber ?? string.Empty).Trim();
        user.NationalId = string.IsNullOrWhiteSpace(model.NationalId) ? null : model.NationalId.Trim();
        user.BillingAddress = string.IsNullOrWhiteSpace(model.BillingAddress) ? null : model.BillingAddress.Trim();

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.Password = PasswordHelper.HashPassword(user, model.Password);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleUser(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        if (user.Role == "Admin")
        {
            return BadRequest("Cannot lock admin");
        }

        user.IsActive = !user.IsActive;

        _context.SaveChanges();

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        if (user.Role == "Admin")
        {
            return BadRequest("Cannot modify admin");
        }

        user.IsDeleted = true;

        _context.SaveChanges();

        return RedirectToAction(nameof(Users));
    }

    public IActionResult Transactions()
    {
        return RedirectToAction("Index", "AdminTransaction");
    }

    public IActionResult Feedbacks()
    {
        return View(_context.Feedbacks
            .Include(x => x.User)
            .Include(x => x.Product)
            .Include(x => x.Transaction)
            .OrderByDescending(x => x.CreatedAt)
            .ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyFeedback(int id, string adminReply)
    {
        var feedback = await _context.Feedbacks
            .FirstOrDefaultAsync(x => x.Id == id);

        if (feedback == null)
        {
            return NotFound();
        }

        var trimmedReply = (adminReply ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedReply))
        {
            TempData["ErrorMessage"] = "Reply content cannot be empty.";
            return RedirectToAction(nameof(Feedbacks));
        }

        var hadExistingReply = !string.IsNullOrWhiteSpace(feedback.AdminReply);
        feedback.AdminReply = trimmedReply;
        feedback.AdminRepliedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = hadExistingReply
            ? "Reply updated successfully."
            : "Reply sent successfully.";
        return RedirectToAction(nameof(Feedbacks));
    }

    private object BuildWeeklyTrends(DateTime from, DateTime to)
    {
        var start = GetWeekStart(from);
        var endBucketStart = GetWeekStart(to);
        var weeks = (int)((endBucketStart - start).TotalDays / 7) + 1;

        if (weeks > 120)
        {
            return new
            {
                period = "week",
                error = "Date range too large for weekly grouping (max 120 weeks)."
            };
        }

        var endExclusive = endBucketStart.AddDays(7);

        var labels = Enumerable.Range(0, weeks)
            .Select(i => start.AddDays(7 * i).ToString("dd/MM", CultureInfo.InvariantCulture))
            .ToArray();

        var userDates = _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role == "User" && u.CreatedAt >= start && u.CreatedAt < endExclusive)
            .Select(u => u.CreatedAt)
            .ToList();

        var transactionDates = _context.Transactions
            .AsNoTracking()
            .Where(t => t.Status == TransactionStatus.Success && t.CreatedAt >= start && t.CreatedAt < endExclusive)
            .Select(t => t.CreatedAt)
            .ToList();

        var users = new int[weeks];
        foreach (var createdAt in userDates)
        {
            var bucketStart = GetWeekStart(createdAt);
            var index = (int)((bucketStart - start).TotalDays / 7);
            if (index >= 0 && index < weeks)
            {
                users[index]++;
            }
        }

        var transactions = new int[weeks];
        foreach (var createdAt in transactionDates)
        {
            var bucketStart = GetWeekStart(createdAt);
            var index = (int)((bucketStart - start).TotalDays / 7);
            if (index >= 0 && index < weeks)
            {
                transactions[index]++;
            }
        }

        return new
        {
            period = "week",
            labels,
            users,
            transactions
        };
    }

    private object BuildMonthlyTrends(DateTime from, DateTime to)
    {
        var start = new DateTime(from.Year, from.Month, 1);
        var endBucketStart = new DateTime(to.Year, to.Month, 1);
        var months = (endBucketStart.Year - start.Year) * 12 + (endBucketStart.Month - start.Month) + 1;

        if (months > 120)
        {
            return new
            {
                period = "month",
                error = "Date range too large for monthly grouping (max 120 months)."
            };
        }

        var endExclusive = endBucketStart.AddMonths(1);

        var labels = Enumerable.Range(0, months)
            .Select(i => start.AddMonths(i).ToString("MM/yyyy", CultureInfo.InvariantCulture))
            .ToArray();

        var userDates = _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role == "User" && u.CreatedAt >= start && u.CreatedAt < endExclusive)
            .Select(u => u.CreatedAt)
            .ToList();

        var transactionDates = _context.Transactions
            .AsNoTracking()
            .Where(t => t.Status == TransactionStatus.Success && t.CreatedAt >= start && t.CreatedAt < endExclusive)
            .Select(t => t.CreatedAt)
            .ToList();

        var users = new int[months];
        foreach (var createdAt in userDates)
        {
            var bucketStart = new DateTime(createdAt.Year, createdAt.Month, 1);
            var index = (bucketStart.Year - start.Year) * 12 + (bucketStart.Month - start.Month);
            if (index >= 0 && index < months)
            {
                users[index]++;
            }
        }

        var transactions = new int[months];
        foreach (var createdAt in transactionDates)
        {
            var bucketStart = new DateTime(createdAt.Year, createdAt.Month, 1);
            var index = (bucketStart.Year - start.Year) * 12 + (bucketStart.Month - start.Month);
            if (index >= 0 && index < months)
            {
                transactions[index]++;
            }
        }

        return new
        {
            period = "month",
            labels,
            users,
            transactions
        };
    }

    private object BuildYearlyTrends(DateTime from, DateTime to)
    {
        var startYear = from.Year;
        var endYear = to.Year;
        var years = endYear - startYear + 1;

        if (years > 50)
        {
            return new
            {
                period = "year",
                error = "Date range too large for yearly grouping (max 50 years)."
            };
        }

        var start = new DateTime(startYear, 1, 1);
        var endExclusive = new DateTime(endYear + 1, 1, 1);

        var labels = Enumerable.Range(0, years)
            .Select(i => (startYear + i).ToString(CultureInfo.InvariantCulture))
            .ToArray();

        var userDates = _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role == "User" && u.CreatedAt >= start && u.CreatedAt < endExclusive)
            .Select(u => u.CreatedAt)
            .ToList();

        var transactionDates = _context.Transactions
            .AsNoTracking()
            .Where(t => t.Status == TransactionStatus.Success && t.CreatedAt >= start && t.CreatedAt < endExclusive)
            .Select(t => t.CreatedAt)
            .ToList();

        var users = new int[years];
        foreach (var createdAt in userDates)
        {
            var index = createdAt.Year - startYear;
            if (index >= 0 && index < years)
            {
                users[index]++;
            }
        }

        var transactions = new int[years];
        foreach (var createdAt in transactionDates)
        {
            var index = createdAt.Year - startYear;
            if (index >= 0 && index < years)
            {
                transactions[index]++;
            }
        }

        return new
        {
            period = "year",
            labels,
            users,
            transactions
        };
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var day = date.Date;
        var diff = ((int)day.DayOfWeek + 6) % 7; // Monday = 0, Sunday = 6
        return day.AddDays(-diff);
    }
}
