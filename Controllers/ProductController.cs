using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;

namespace Online_Mobile_Recharge.Controllers
{
    public class ProductController : Controller
    {
        private const string SelectedPhoneSessionKey = "SelectedPhoneNumber";
        private readonly MobileRechargeDbContext _context;
        private readonly FeedbackService _feedbackService;

        public ProductController(MobileRechargeDbContext context, FeedbackService feedbackService)
        {
            _context = context;
            _feedbackService = feedbackService;
        }

        public IActionResult Index()
        {
            ViewBag.SelectedPhoneNumber = string.Empty;
            ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;

            var now = DateTime.Now;
            var productsQuery = _context.Products.AsNoTracking();

            // Guest users (not logged in) can only use topup products.
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                productsQuery = productsQuery.Where(x => x.Type == ProductType.Card);
            }

            var products = productsQuery.ToList();
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

            products = products
                .OrderByDescending(x => activeSales.ContainsKey(x.Id))
                .ThenByDescending(x => x.IsTop)
                .ThenByDescending(x => x.IsSpecial)
                .ThenBy(x => x.Type)
                .ThenBy(x => x.Price)
                .ToList();

            var campaignProducts = products
                .Where(x => activeSales.ContainsKey(x.Id))
                .Take(8)
                .ToList();

            var pricingByProductId = products.ToDictionary(
                x => x.Id,
                x =>
                {
                    activeSales.TryGetValue(x.Id, out var sale);
                    return new ProductPricingInfoViewModel
                    {
                        ProductId = x.Id,
                        OriginalPrice = x.Price,
                        EffectivePrice = ProductSaleCalculator.GetEffectivePrice(x.Price, sale, now),
                        HasSale = ProductSaleCalculator.IsActive(sale, now),
                        SaleBadgeText = ProductSaleCalculator.GetBadgeText(sale, now)
                    };
                });

            ViewBag.PricingByProductId = pricingByProductId;
            ViewBag.CampaignProducts = campaignProducts;
            return View(products);
        }

        public IActionResult Detail(int id)
        {
            var product = _context.Products.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Guest users can only view/use topup products.
            if (HttpContext.Session.GetInt32("UserId") == null && product.Type != ProductType.Card)
            {
                TempData["ErrorMessage"] = "Guest users can only use topup (recharge) products. Please login to access other services.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;
            var activeSale = _context.ProductSales
                .AsNoTracking()
                .Where(x =>
                    x.ProductId == id &&
                    x.SaleType != ProductSaleType.None &&
                    x.SaleValue > 0 &&
                    (x.StartAt == null || x.StartAt <= now) &&
                    (x.EndAt == null || x.EndAt >= now))
                .OrderByDescending(x => x.StartAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            ViewBag.Pricing = new ProductPricingInfoViewModel
            {
                ProductId = product.Id,
                OriginalPrice = product.Price,
                EffectivePrice = ProductSaleCalculator.GetEffectivePrice(product.Price, activeSale, now),
                HasSale = ProductSaleCalculator.IsActive(activeSale, now),
                SaleBadgeText = ProductSaleCalculator.GetBadgeText(activeSale, now)
            };

            var userId = HttpContext.Session.GetInt32("UserId");
            var myFeedback = userId == null ? null : _feedbackService.GetUserProductFeedback(userId.Value, id);
            var hasPurchased = userId != null && _feedbackService.HasPurchasedProduct(userId.Value, id);

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                Feedbacks = _feedbackService.GetProductFeedbacks(id),
                MyFeedback = myFeedback,
                IsLoggedIn = userId != null,
                HasPurchased = hasPurchased,
                CanSubmitFeedback = hasPurchased,
                CanEditFeedback = myFeedback != null && DateTime.Now <= myFeedback.CreatedAt.AddDays(1),
                EditDeadline = myFeedback?.CreatedAt.AddDays(1),
                FeedbackForm = new ProductFeedbackFormViewModel
                {
                    ProductId = id,
                    Message = myFeedback?.Message ?? string.Empty
                }
            };

            return View(viewModel);
        }

        public IActionResult Buy(int id)
        {
            var product = _context.Products.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                return RedirectToAction("Index", "Checkout", new { productId = id });
            }

            if (product.Type != ProductType.Card)
            {
                TempData["ErrorMessage"] = "Please login to purchase this product.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Detail", "Product", new { id }) });
            }

            return RedirectToAction("Index", "GuestCheckout", new { productId = id });
        }
    }
}
