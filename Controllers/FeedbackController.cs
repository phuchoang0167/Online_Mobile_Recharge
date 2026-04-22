using Microsoft.AspNetCore.Mvc;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;

namespace Online_Mobile_Recharge.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly FeedbackService _service;
        private readonly EmailNotificationService _emailNotificationService;
        private readonly MobileRechargeDbContext _context;

        public FeedbackController(
            FeedbackService service,
            EmailNotificationService emailNotificationService,
            MobileRechargeDbContext context)
        {
            _service = service;
            _emailNotificationService = emailNotificationService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var model = new FeedbackCenterViewModel
            {
                IsLoggedIn = userId != null,
                MyFeedbacks = userId == null ? new List<Feedback>() : _service.GetUserProductFeedbacks(userId.Value)
            };

            if (userId != null)
            {
                model.PurchasedProductsCount = _context.Transactions
                    .Where(x => x.UserId == userId && x.Status == TransactionStatus.Success && x.ProductId != null)
                    .Select(x => x.ProductId)
                    .Distinct()
                    .Count();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public async Task<IActionResult> Upsert([Bind(Prefix = "FeedbackForm")] ProductFeedbackFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["FeedbackError"] = "Invalid feedback content.";
                return RedirectToAction("Detail", "Product", new { id = model.ProductId });
            }

            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var result = _service.UpsertProductFeedback(userId, model.ProductId, model.Message);
            if (!result.IsSuccess)
            {
                TempData["FeedbackError"] = result.ErrorMessage;
                return RedirectToAction("Detail", "Product", new { id = model.ProductId });
            }

            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (user != null)
            {
                await _emailNotificationService.SendContactConfirmationAsync(
                    user.Name,
                    user.Email,
                    "product feedback");
            }

            TempData["SuccessMessage"] = "Feedback saved successfully.";
            return RedirectToAction("Detail", "Product", new { id = model.ProductId });
        }
    }
}
