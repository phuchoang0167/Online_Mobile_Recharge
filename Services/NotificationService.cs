using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;

namespace Online_Mobile_Recharge.Services;

public class NotificationService
{
    private readonly MobileRechargeDbContext _context;

    public NotificationService(MobileRechargeDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationBellViewModel> BuildAsync(int? userId, string? role)
    {
        var viewModel = new NotificationBellViewModel();

        if (userId == null || string.IsNullOrWhiteSpace(role))
        {
            return viewModel;
        }

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            viewModel.Heading = "Admin notifications";
            viewModel.Items = await BuildAdminItemsAsync();
            viewModel.StorageKey = "notification-state:admin";
        }
        else
        {
            viewModel.Heading = "Your notifications";
            viewModel.Items = await BuildUserItemsAsync(userId.Value);
            viewModel.StorageKey = $"notification-state:user:{userId.Value}";
        }

        viewModel.Count = viewModel.Items.Count;
        viewModel.CookieKey = BuildCookieKey(viewModel.StorageKey);
        viewModel.Signature = string.Join("||", viewModel.Items.Select(x => $"{x.Title}|{x.Description}|{x.Url}|{x.Tone}"));
        return viewModel;
    }

    private static string BuildCookieKey(string storageKey)
    {
        return string.IsNullOrWhiteSpace(storageKey)
            ? string.Empty
            : $"bell-state-{storageKey.Replace(':', '-')}";
    }

    private async Task<List<NotificationItemViewModel>> BuildAdminItemsAsync()
    {
        var items = new List<NotificationItemViewModel>();
        var now = DateTime.Now;

        var pendingTransactions = await _context.Transactions
            .CountAsync(x => x.Status == TransactionStatus.Pending);
        if (pendingTransactions > 0)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = $"{pendingTransactions} pending transaction(s)",
                Description = "Review postpaid bills and transactions that need admin attention.",
                Url = "/Admin/Transaction",
                Icon = "bi-clock-history",
                Tone = "warning"
            });
        }

        var overdueBills = await _context.Transactions
            .CountAsync(x => x.Status == TransactionStatus.Pending && x.DueDate != null && x.DueDate < now);
        if (overdueBills > 0)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = $"{overdueBills} overdue bill(s)",
                Description = "Prioritize reminders and follow-up for overdue pending bills.",
                Url = "/Admin/Transaction",
                Icon = "bi-exclamation-triangle",
                Tone = "danger"
            });
        }

        var productFeedbacks = await _context.Feedbacks
            .CountAsync(x => x.Category == FeedbackService.ProductReviewCategory);
        if (productFeedbacks > 0)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = $"{productFeedbacks} product review(s)",
                Description = "Review post-purchase feedback to understand user experience.",
                Url = "/Admin/Feedbacks",
                Icon = "bi-chat-dots",
                Tone = "info"
            });
        }

        var supportRequests = await _context.Feedbacks
            .CountAsync(x => x.Category == FeedbackService.SupportCategory);
        if (supportRequests > 0)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = $"{supportRequests} support request(s)",
                Description = "New contact messages and support feedback are waiting for review.",
                Url = "/Admin/Feedbacks",
                Icon = "bi-envelope-paper",
                Tone = "secondary"
            });
        }

        var inactiveUsers = await _context.Users
            .CountAsync(x => !x.IsDeleted && !x.IsActive);
        if (inactiveUsers > 0)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = $"{inactiveUsers} locked account(s)",
                Description = "Some user accounts are locked and may need review or reactivation.",
                Url = "/Admin/Users",
                Icon = "bi-person-lock",
                Tone = "danger"
            });
        }

        return items.Take(5).ToList();
    }

    private async Task<List<NotificationItemViewModel>> BuildUserItemsAsync(int userId)
    {
        var items = new List<NotificationItemViewModel>();
        var now = DateTime.Now;

        var pendingTransactions = await _context.Transactions
            .CountAsync(x => x.UserId == userId && x.Type == TransactionType.Postpaid && x.Status == TransactionStatus.Pending);
        if (pendingTransactions > 0)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = $"{pendingTransactions} pending postpaid bill(s)",
                Description = "You have pending bills that still need to be paid.",
                Url = "/User/Transaction",
                Icon = "bi-receipt",
                Tone = "warning"
            });
        }

        var overdueBills = await _context.Transactions
            .CountAsync(x =>
                x.UserId == userId &&
                x.Type == TransactionType.Postpaid &&
                x.Status == TransactionStatus.Pending &&
                x.DueDate != null &&
                x.DueDate < now);
        if (overdueBills > 0)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = $"{overdueBills} overdue bill(s)",
                Description = "Pay soon to avoid missing your postpaid bills.",
                Url = "/User/Transaction",
                Icon = "bi-alarm",
                Tone = "danger"
            });
        }

        var dueSoonBills = await _context.Transactions
            .CountAsync(x =>
                x.UserId == userId &&
                x.Type == TransactionType.Postpaid &&
                x.Status == TransactionStatus.Pending &&
                x.DueDate != null &&
                x.DueDate >= now &&
                x.DueDate <= now.AddDays(2));
        if (dueSoonBills > 0)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = $"{dueSoonBills} bill(s) due soon",
                Description = "Your postpaid bill will reach its due date within the next 2 days.",
                Url = "/User/Transaction",
                Icon = "bi-hourglass-split",
                Tone = "warning"
            });
        }

        var dndStatus = await _context.DndSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
        if (dndStatus?.IsEnabled == true)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = "DND is enabled",
                Description = $"Current mode: {dndStatus.Mode}. You can review the blocked list anytime.",
                Url = "/User/Dnd",
                Icon = "bi-shield-check",
                Tone = "success"
            });
        }

        var callerTuneSetting = await _context.UserCallerTuneSettings
            .AsNoTracking()
            .Include(x => x.CallerTune)
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.IsEnabled &&
                x.CallerTuneId != null);

        if (callerTuneSetting != null)
        {
            var hasActiveCallerTuneSubscription = await _context.CallerTuneSubscriptions
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.ExpiresAt > now);

            if (hasActiveCallerTuneSubscription)
            {
                items.Add(new NotificationItemViewModel
                {
                    Title = "Caller tune is active",
                    Description = string.IsNullOrWhiteSpace(callerTuneSetting.CallerTune?.Title)
                        ? "You currently have an active caller tune. You can change or turn it off anytime."
                        : $"Current tune: {callerTuneSetting.CallerTune.Title}. You can change or turn it off anytime.",
                    Url = "/User/CallerTune",
                    Icon = "bi-music-note-beamed",
                    Tone = "primary"
                });
            }
            else
            {
                items.Add(new NotificationItemViewModel
                {
                    Title = "Caller tune subscription expired",
                    Description = "Renew the monthly plan to keep using your selected caller tune.",
                    Url = "/User/CallerTune",
                    Icon = "bi-music-note-beamed",
                    Tone = "warning"
                });
            }
        }

        var expiredCards = await _context.Cards
            .CountAsync(x => x.UserId == userId && x.ExpiryDate.Date < DateTime.Today);
        if (expiredCards > 0)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = $"{expiredCards} expired card(s)",
                Description = "Update or remove expired cards before your next payment.",
                Url = "/User/Cards",
                Icon = "bi-credit-card-2-front",
                Tone = "dark"
            });
        }

        var noCards = await _context.Cards
            .CountAsync(x => x.UserId == userId) == 0;
        if (noCards)
        {
            items.Add(new NotificationItemViewModel
            {
                Title = "Cards unavailable",
                Description = "Payment cards are required so prepaid and postpaid payments can be validated correctly.",
                Url = "/User/Cards",
                Icon = "bi-plus-circle",
                Tone = "info"
            });
        }

        var profile = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);
        if (profile != null && string.IsNullOrWhiteSpace(profile.PhoneNumber))
        {
            items.Add(new NotificationItemViewModel
            {
                Title = "Complete your profile",
                Description = "Add your phone number so checkout and bill payments work more smoothly.",
                Url = "/User/Profile",
                Icon = "bi-person-lines-fill",
                Tone = "dark"
            });
        }

        return items.Take(5).ToList();
    }
}
