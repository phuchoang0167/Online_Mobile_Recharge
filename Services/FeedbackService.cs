using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;

namespace Online_Mobile_Recharge.Services
{
    public class FeedbackService
    {
        public const string SupportCategory = "Support";
        public const string ProductReviewCategory = "ProductReview";

        private readonly MobileRechargeDbContext _context;

        public FeedbackService(MobileRechargeDbContext context)
        {
            _context = context;
        }

        public void CreateSupport(string name, string email, string message, int? userId = null)
        {
            var fb = new Feedback
            {
                Category = SupportCategory,
                Name = name.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                Message = message.Trim(),
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.Feedbacks.Add(fb);
            _context.SaveChanges();
        }

        public List<Feedback> GetProductFeedbacks(int productId)
        {
            return _context.Feedbacks
                .AsNoTracking()
                .Where(x => x.Category == ProductReviewCategory && x.ProductId == productId)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ToList();
        }

        public List<Feedback> GetUserProductFeedbacks(int userId)
        {
            return _context.Feedbacks
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.Category == ProductReviewCategory && x.UserId == userId)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ToList();
        }

        public bool HasPurchasedProduct(int userId, int productId)
        {
            return _context.Transactions.Any(x =>
                x.UserId == userId &&
                x.ProductId == productId &&
                x.Status == TransactionStatus.Success);
        }

        public Feedback? GetUserProductFeedback(int userId, int productId)
        {
            return _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefault(x =>
                    x.Category == ProductReviewCategory &&
                    x.UserId == userId &&
                    x.ProductId == productId);
        }

        public (bool IsSuccess, string? ErrorMessage) UpsertProductFeedback(int userId, int productId, string message)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == userId && !x.IsDeleted && x.IsActive);
            if (user == null)
            {
                return (false, "A valid account could not be found.");
            }

            var latestSuccessfulTransaction = _context.Transactions
                .Where(x =>
                    x.UserId == userId &&
                    x.ProductId == productId &&
                    x.Status == TransactionStatus.Success)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (latestSuccessfulTransaction == null)
            {
                return (false, "You can leave feedback only after a successful product purchase.");
            }

            var existingFeedback = _context.Feedbacks
                .FirstOrDefault(x =>
                    x.Category == ProductReviewCategory &&
                    x.UserId == userId &&
                    x.ProductId == productId);

            if (existingFeedback == null)
            {
                _context.Feedbacks.Add(new Feedback
                {
                    Category = ProductReviewCategory,
                    Name = user.Name,
                    Email = user.Email,
                    Message = message.Trim(),
                    UserId = user.Id,
                    ProductId = productId,
                    TransactionId = latestSuccessfulTransaction.Id,
                    CreatedAt = DateTime.Now
                });

                _context.SaveChanges();
                return (true, null);
            }

            if (DateTime.Now > existingFeedback.CreatedAt.AddDays(1))
            {
                return (false, "Feedback can only be edited within 1 day of submission.");
            }

            existingFeedback.Message = message.Trim();
            existingFeedback.UpdatedAt = DateTime.Now;
            existingFeedback.TransactionId = latestSuccessfulTransaction.Id;

            _context.SaveChanges();
            return (true, null);
        }
    }
}
