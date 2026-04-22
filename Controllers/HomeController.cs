using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;

namespace Online_Mobile_Recharge.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MobileRechargeDbContext _context;
        private readonly FeedbackService _feedbackService;
        private readonly EmailNotificationService _emailNotificationService;

        public HomeController(
            ILogger<HomeController> logger,
            MobileRechargeDbContext context,
            FeedbackService feedbackService,
            EmailNotificationService emailNotificationService)
        {
            _logger = logger;
            _context = context;
            _feedbackService = feedbackService;
            _emailNotificationService = emailNotificationService;
        }

        public IActionResult Index()
        {
            var now = DateTime.Now;
            var totalTransactions = _context.Transactions.Count();
            var successTransactions = _context.Transactions.Count(x => x.Status == TransactionStatus.Success);

            var bestSellerProducts = (
                from product in _context.Products
                join stats in (
                    from transaction in _context.Transactions
                    where transaction.Status == TransactionStatus.Success && transaction.ProductId != null
                    group transaction by transaction.ProductId into grouped
                    select new
                    {
                        ProductId = grouped.Key!.Value,
                        SoldCount = grouped.Count(),
                        LatestSaleAt = grouped.Max(x => x.CreatedAt)
                    }
                ) on product.Id equals stats.ProductId
                orderby stats.SoldCount descending, stats.LatestSaleAt descending, product.Price ascending
                select product
            )
            .Take(4)
            .ToList();

            if (bestSellerProducts.Count < 4)
            {
                var bestSellerProductIds = bestSellerProducts.Select(x => x.Id).ToList();
                var fallbackProducts = _context.Products
                    .Where(x => !bestSellerProductIds.Contains(x.Id))
                    .OrderByDescending(x => x.IsTop)
                    .ThenByDescending(x => x.IsSpecial)
                    .ThenBy(x => x.Price)
                    .Take(4 - bestSellerProducts.Count)
                    .ToList();

                bestSellerProducts.AddRange(fallbackProducts);
            }

            var model = new HomeViewModel
            {
                TotalUsers = _context.Users.Count(x => !x.IsDeleted),
                TotalTransactions = totalTransactions,
                SuccessRate = totalTransactions == 0
                    ? 100
                    : Math.Round((decimal)successTransactions / totalTransactions * 100, 1),
                FeaturedProducts = bestSellerProducts,
                FaqItems = BuildFaqItems().Take(4).ToList()
            };

            var featuredProductIds = bestSellerProducts.Select(x => x.Id).ToList();
            var activeSales = _context.ProductSales
                .AsNoTracking()
                .Where(x =>
                    featuredProductIds.Contains(x.ProductId) &&
                    x.SaleType != ProductSaleType.None &&
                    x.SaleValue > 0 &&
                    (x.StartAt == null || x.StartAt <= now) &&
                    (x.EndAt == null || x.EndAt >= now))
                .OrderByDescending(x => x.StartAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .ToList()
                .GroupBy(x => x.ProductId)
                .ToDictionary(x => x.Key, x => x.First());

            ViewBag.FeaturedPricingByProductId = bestSellerProducts.ToDictionary(
                x => x.Id,
                x =>
                {
                    activeSales.TryGetValue(x.Id, out var sale);
                    return new ProductPricingInfoViewModel
                    {
                        ProductId = x.Id,
                        OriginalPrice = x.Price,
                        EffectivePrice = ProductSaleCalculator.GetEffectivePrice(x.Price, sale),
                        HasSale = ProductSaleCalculator.IsActive(sale, now),
                        SaleBadgeText = ProductSaleCalculator.GetBadgeText(sale)
                    };
                });

            return View(model);
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult HowItWorks()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Terms()
        {
            return View();
        }

        [HttpGet]
        public IActionResult PostpaidContract()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Sitemap()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Faq()
        {
            return View(BuildFaqItems());
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            _feedbackService.CreateSupport(model.Name, model.Email, model.Message, currentUserId);
            var emailSent = await _emailNotificationService.SendContactConfirmationAsync(
                model.Name,
                model.Email,
                "support request");
            await _emailNotificationService.SendSupportInboxNotificationAsync(
                model.Name,
                model.Email,
                model.Message,
                "contact message");

            TempData["SuccessMessage"] = emailSent
                ? "Your message has been sent successfully. A confirmation email was also sent."
                : "Your message has been sent successfully.";

            return RedirectToAction(nameof(Contact));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static List<FaqItemViewModel> BuildFaqItems()
        {
            return new List<FaqItemViewModel>
            {
                new()
                {
                    Question = "How do I recharge a prepaid number?",
                    Answer = "Open Products, choose a package or top-up card, continue to checkout, and confirm the phone number before payment."
                },
                new()
                {
                    Question = "Can I pay postpaid bills through this platform?",
                    Answer = "Yes. Create the postpaid bill at checkout first, then settle the pending bill later from your transaction history by entering the card details manually."
                },
                new()
                {
                    Question = "What if I forget my password?",
                    Answer = "Use the reset password page from the login screen."
                },
                new()
                {
                    Question = "What does DND mode do?",
                    Answer = "DND lets you allow all calls, block all calls, or manage a custom list of allowed or blocked numbers depending on your preference."
                },
                new()
                {
                    Question = "Where can I review my transaction history?",
                    Answer = "Log in, open your dashboard, and use the transaction history screen to see the latest recharge and bill payment records."
                }
            };
        }
    }
}
