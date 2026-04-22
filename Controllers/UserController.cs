using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;

[UserAuthorize]
public class UserController : Controller
{
    private readonly MobileRechargeDbContext _context;

    public UserController(MobileRechargeDbContext context)
    {
        _context = context;
    }

    public IActionResult Dashboard()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var today = DateTime.Now;
        var dueSoonDate = today.AddDays(2);

        ViewBag.TotalTransactions = _context.Transactions.Count(x => x.UserId == userId);

        ViewBag.TotalSpent = _context.Transactions
            .Where(x => x.UserId == userId && x.Status == TransactionStatus.Success)
            .Select(x => (decimal?)x.Amount)
            .Sum() ?? 0;

        ViewBag.SavedCards = _context.Cards.Count(x => x.UserId == userId);
        ViewBag.AvailableBalance = _context.Cards
            .Where(x => x.UserId == userId)
            .Select(x => (decimal?)x.Balance)
            .Sum() ?? 0;

        ViewBag.PendingBills = _context.Transactions.Count(x =>
            x.UserId == userId &&
            x.Type == TransactionType.Postpaid &&
            x.Status == TransactionStatus.Pending &&
            !x.IsPaid);

        ViewBag.PendingAmount = _context.Transactions
            .Where(x =>
                x.UserId == userId &&
                x.Type == TransactionType.Postpaid &&
                x.Status == TransactionStatus.Pending &&
                !x.IsPaid)
            .Select(x => (decimal?)x.Amount)
            .Sum() ?? 0;

        ViewBag.DueSoonBills = _context.Transactions.Count(x =>
            x.UserId == userId &&
            x.Type == TransactionType.Postpaid &&
            x.Status == TransactionStatus.Pending &&
            !x.IsPaid &&
            x.DueDate != null &&
            x.DueDate >= today &&
            x.DueDate <= dueSoonDate);

        ViewBag.OverdueBills = _context.Transactions.Count(x =>
            x.UserId == userId &&
            x.Type == TransactionType.Postpaid &&
            x.Status == TransactionStatus.Pending &&
            !x.IsPaid &&
            x.DueDate != null &&
            x.DueDate < today);

        ViewBag.LatestDueSoonBills = _context.Transactions
            .Include(x => x.Product)
            .Where(x =>
                x.UserId == userId &&
                x.Type == TransactionType.Postpaid &&
                x.Status == TransactionStatus.Pending &&
                !x.IsPaid &&
                x.DueDate != null &&
                x.DueDate >= today &&
                x.DueDate <= dueSoonDate)
            .OrderBy(x => x.DueDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(3)
            .ToList();

        ViewBag.LatestPendingBill = _context.Transactions
            .Include(x => x.Product)
            .Where(x =>
                x.UserId == userId &&
                x.Type == TransactionType.Postpaid &&
                x.Status == TransactionStatus.Pending &&
                !x.IsPaid)
            .OrderBy(x => x.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        ViewBag.MyFeedbacks = _context.Feedbacks.Count(x =>
            x.UserId == userId &&
            x.Category == FeedbackService.ProductReviewCategory);

        ViewBag.RecentFeedbacks = _context.Feedbacks
            .Include(x => x.Product)
            .Where(x =>
                x.UserId == userId &&
                x.Category == FeedbackService.ProductReviewCategory)
            .OrderByDescending(x => x.CreatedAt)
            .Take(3)
            .ToList();

        ViewBag.LastSuccessfulTransaction = _context.Transactions
            .Include(x => x.Product)
            .Where(x => x.UserId == userId && x.Status == TransactionStatus.Success)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        return View();
    }

    public IActionResult DataPackages()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var now = DateTime.Now;

        var phones = _context.Transactions
            .Include(x => x.Product)
            .Where(x =>
                x.UserId == userId &&
                x.Status == TransactionStatus.Success &&
                x.Product != null &&
                x.Product.Type == ProductType.Data)
            .Select(x => x.PhoneNumber)
            .Distinct()
            .ToList();

        var subscriptions = _context.PhoneDataSubscriptions
            .Include(x => x.Product)
            .Where(x => phones.Contains(x.PhoneNumber) && x.ExpiresAt > now)
            .OrderBy(x => x.PhoneNumber)
            .ThenByDescending(x => x.ExpiresAt)
            .ToList();

        var groups = subscriptions
            .GroupBy(x => x.PhoneNumber)
            .Select(group => new ActiveDataPackageGroupViewModel
            {
                PhoneNumber = group.Key,
                Items = group.Select(x => new ActiveDataPackageItemViewModel
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product?.Name ?? $"Package #{x.ProductId}",
                    ActivatedAt = x.ActivatedAt,
                    ExpiresAt = x.ExpiresAt
                }).ToList()
            })
            .ToList();

        return View(new ActiveDataPackagesViewModel
        {
            Now = now,
            Groups = groups
        });
    }

    public IActionResult Profile()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(BuildProfileViewModel(user));
    }

    public IActionResult Settings()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);

        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(BuildSettingsViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettings(UserSettingsViewModel model)
    {
        var currentUserId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);

        if (user == null)
        {
            return NotFound();
        }

        model.Name = (model.Name ?? string.Empty).Trim();
        model.PhoneNumber = (model.PhoneNumber ?? string.Empty).Trim();
        model.Email = user.Email;
        model.EmailVerified = user.EmailVerified;
        model.CreatedAt = user.CreatedAt;

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Name is required.");
        }

        if (!ModelState.IsValid)
        {
            return View("Settings", model);
        }

        user.Name = (model.Name ?? string.Empty).Trim();
        user.PhoneNumber = (model.PhoneNumber ?? string.Empty).Trim();

        await _context.SaveChangesAsync();

        HttpContext.Session.SetString("Name", user.Name);

        TempData["Success"] = "Your account details have been updated.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(UserSettingsViewModel model)
    {
        var currentUserId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);

        if (user == null)
        {
            return NotFound();
        }

        var settingsModel = BuildSettingsViewModel(user);

        if (string.IsNullOrWhiteSpace(model.CurrentPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Please enter your current password.");
        }

        if (string.IsNullOrWhiteSpace(model.NewPassword))
        {
            ModelState.AddModelError(nameof(model.NewPassword), "Please enter a new password.");
        }
        else if (model.NewPassword.Length < 6)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "New password must be at least 6 characters.");
        }

        if (string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
        {
            ModelState.AddModelError(nameof(model.ConfirmNewPassword), "Please confirm your new password.");
        }
        else if (!string.Equals(model.NewPassword, model.ConfirmNewPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.ConfirmNewPassword), "New password confirmation does not match.");
        }

        if (!string.IsNullOrWhiteSpace(model.CurrentPassword))
        {
            var passwordCheck = PasswordHelper.VerifyPassword(user, model.CurrentPassword);
            if (!passwordCheck.IsValid)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Settings", settingsModel);
        }

        user.Password = PasswordHelper.HashPassword(user, model.NewPassword);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction(nameof(Settings));
    }

    private static UserProfileViewModel BuildProfileViewModel(User user) => new()
    {
        Name = user.Name,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        EmailVerified = user.EmailVerified,
        CreatedAt = user.CreatedAt
    };

    private static UserSettingsViewModel BuildSettingsViewModel(User user) => new()
    {
        Name = user.Name,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        EmailVerified = user.EmailVerified,
        CreatedAt = user.CreatedAt
    };
}
