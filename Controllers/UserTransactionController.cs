using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;

[Route("User/Transaction")]
[UserAuthorize]
public class UserTransactionController : Controller
{
    private readonly TransactionService _service;
    private readonly MobileRechargeDbContext _context;
    private readonly EmailNotificationService _emailNotificationService;

    public UserTransactionController(
        TransactionService service,
        MobileRechargeDbContext context,
        EmailNotificationService emailNotificationService)
    {
        _service = service;
        _context = context;
        _emailNotificationService = emailNotificationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? keyword, string? status, string? type, string? range)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var data = await _service.GetTransactionsAsync(userId, keyword, status, type, range);
        var savedCards = BuildSavedCards(userId);

        return View(new TransactionListViewModel
        {
            Keyword = keyword?.Trim() ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(status) ? "all" : status,
            Type = string.IsNullOrWhiteSpace(type) ? "all" : type,
            Range = string.IsNullOrWhiteSpace(range) ? "all" : range,
            Items = data,
            SavedCards = savedCards,
            DefaultSavedCardId = savedCards.FirstOrDefault()?.Id
        });
    }

    [HttpGet("Pay/{id:int}")]
    public IActionResult Pay(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var transaction = _service.GetUserTransactionById(id, userId);

        if (transaction == null ||
            transaction.Type != TransactionType.Postpaid ||
            transaction.Status != TransactionStatus.Pending)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(BuildPayViewModel(transaction, userId));
    }

    [HttpGet("Invoice/{id:int}")]
    public IActionResult Invoice(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var transaction = _context.Transactions
            .Include(x => x.Product)
            .FirstOrDefault(x => x.Id == id && x.UserId == userId);
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);

        if (transaction == null || user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(new TransactionInvoiceViewModel
        {
            Transaction = transaction,
            User = user,
            ProductName = transaction.Product?.Name ?? "Service transaction"
        });
    }

    [HttpGet("Pay")]
    public IActionResult Pay()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Pay")]
    [HttpPost("Pay/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int? id, SettlePostpaidViewModel model, bool inline = false)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        model.TransactionId = model.TransactionId > 0
            ? model.TransactionId
            : id.GetValueOrDefault();

        string? cardNumber = model.CardNumber;
        string? cvv = model.CVV;
        string? expiry = model.Expiry;

        model.CardInputMode = (model.CardInputMode ?? string.Empty).Trim().ToLowerInvariant();
        if (model.CardInputMode == "saved")
        {
            ModelState.Remove(nameof(model.CardNumber));
            ModelState.Remove(nameof(model.CVV));
            ModelState.Remove(nameof(model.Expiry));

            if (!model.SavedCardId.HasValue)
            {
                ModelState.AddModelError(nameof(model.SavedCardId), "Please choose a saved card.");
            }
            else
            {
                var selectedCard = _context.Cards.FirstOrDefault(x => x.Id == model.SavedCardId.Value && x.UserId == userId);
                if (selectedCard == null)
                {
                    ModelState.AddModelError(nameof(model.SavedCardId), "Saved card not found.");
                }
                else
                {
                    cardNumber = selectedCard.CardNumber;
                    cvv = selectedCard.CVV;
                    expiry = selectedCard.ExpiryDate.ToString("MM/yy");
                }
            }
        }
        else if (model.CardInputMode == "manual")
        {
            ModelState.Remove(nameof(model.SavedCardId));
        }
        else
        {
            ModelState.AddModelError(nameof(model.CardInputMode), "Invalid card input mode.");
        }

        if (!ModelState.IsValid)
        {
            if (inline)
            {
                TempData["ErrorMessage"] = "Please check the payment form and try again.";
                return RedirectToAction(nameof(Index));
            }

            PopulateTransactionDetails(model, userId);
            return View(model);
        }

        try
        {
            var result = _service.SettlePostpaid(
                model.TransactionId,
                userId,
                cardNumber,
                cvv,
                expiry);

            if (!result.IsSuccess || result.Transaction == null)
            {
                if (inline)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "We could not process this bill payment.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "We could not process this bill payment.");
                PopulateTransactionDetails(model, userId);
                return View(model);
            }

            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            var transaction = _service.GetUserTransactionById(result.Transaction.Id, userId);
            if (user != null && transaction != null)
            {
                await _emailNotificationService.SendTransactionConfirmationAsync(
                    user,
                    transaction,
                    transaction.Product?.Name ?? "Postpaid bill");
            }

            TempData["SuccessMessage"] = "Your postpaid bill was paid successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["ErrorMessage"] = "Something went wrong while processing this bill payment. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("SendReminder/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReminder(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var transaction = _service.GetUserTransactionById(id, userId);

        if (transaction == null ||
            transaction.Type != TransactionType.Postpaid ||
            transaction.Status != TransactionStatus.Pending)
        {
            TempData["ErrorMessage"] = "We could not find a pending postpaid bill to remind you about.";
            return RedirectToAction(nameof(Index));
        }

        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "We could not find your account to send the email.";
            return RedirectToAction(nameof(Index));
        }

        var sent = await _emailNotificationService.SendPostpaidReminderAsync(
            user,
            transaction,
            transaction.Product?.Name ?? "Postpaid bill");

        TempData[sent ? "SuccessMessage" : "ErrorMessage"] = sent
            ? "A payment reminder email has been sent."
            : "We could not send the email. Please check SMTP settings.";

        return RedirectToAction(nameof(Index));
    }

    private SettlePostpaidViewModel BuildPayViewModel(Online_Mobile_Recharge.Models.Entities.Transaction transaction, int userId)
    {
        var savedCards = BuildSavedCards(userId);
        return new SettlePostpaidViewModel
        {
            TransactionId = transaction.Id,
            PhoneNumber = transaction.PhoneNumber,
            Amount = transaction.Amount,
            DueDate = transaction.DueDate,
            ProductName = transaction.Product?.Name ?? "Postpaid bill",
            CardInputMode = savedCards.Any() ? "saved" : "manual",
            SavedCards = savedCards,
            SavedCardId = savedCards.FirstOrDefault()?.Id
        };
    }

    private void PopulateTransactionDetails(SettlePostpaidViewModel model, int userId)
    {
        var transaction = _service.GetUserTransactionById(model.TransactionId, userId);
        if (transaction == null)
        {
            return;
        }

        model.PhoneNumber = transaction.PhoneNumber;
        model.Amount = transaction.Amount;
        model.DueDate = transaction.DueDate;
        model.ProductName = transaction.Product?.Name ?? "Postpaid bill";

        model.SavedCards = BuildSavedCards(userId);
        if (model.SavedCards.Any() && model.SavedCardId == null)
        {
            model.SavedCardId = model.SavedCards[0].Id;
        }

        if (!model.SavedCards.Any())
        {
            model.CardInputMode = "manual";
        }
    }

    private List<SavedCardOptionViewModel> BuildSavedCards(int userId)
    {
        return _context.Cards
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Balance)
            .ThenBy(x => x.ExpiryDate)
            .Select(card => new SavedCardOptionViewModel
            {
                Id = card.Id,
                MaskedNumber = Mask(card.CardNumber),
                Expiry = card.ExpiryDate.ToString("MM/yy"),
                Balance = card.Balance,
                Label = $"{Mask(card.CardNumber)} • exp {card.ExpiryDate:MM/yy} • {card.Balance:N0} VND"
            })
            .ToList();
    }

    private static string Mask(string? cardNumber)
    {
        var normalized = (cardNumber ?? string.Empty).Trim();
        if (normalized.Length >= 4)
        {
            return $"**** **** **** {normalized[^4..]}";
        }

        return "****";
    }
}
