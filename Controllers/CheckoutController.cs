using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;
using System.Text.RegularExpressions;
using System.Text.Json;
[UserAuthorize]
public class CheckoutController : Controller
{
    private const string SelectedPhoneSessionKey = "SelectedPhoneNumber";
    private const string CheckoutDraftSessionPrefix = "CheckoutDraft:";
    private readonly MobileRechargeDbContext _context;
    private readonly TransactionService _service;
    private readonly EmailNotificationService _emailNotificationService;

    public CheckoutController(
        MobileRechargeDbContext context,
        TransactionService service,
        EmailNotificationService emailNotificationService)
    {
        _context = context;
        _service = service;
        _emailNotificationService = emailNotificationService;
    }

    private void LoadData(CheckoutViewModel model, int userId)
    {
        var product = _context.Products.Find(model.ProductId);
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        var recentPhones = _context.Transactions
            .AsNoTracking()
            .Where(x => x.UserId == userId && !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.PhoneNumber)
            .Distinct()
            .Take(2)
            .ToList();
        var cards = _context.Cards
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Balance)
            .ThenBy(x => x.ExpiryDate)
            .ToList();

        if (product != null)
        {
            var now = DateTime.Now;
            var activeSale = _context.ProductSales
                .AsNoTracking()
                .Where(x =>
                    x.ProductId == product.Id &&
                    x.SaleType != ProductSaleType.None &&
                    x.SaleValue > 0 &&
                    (x.StartAt == null || x.StartAt <= now) &&
                    (x.EndAt == null || x.EndAt >= now))
                .OrderByDescending(x => x.StartAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            model.ProductName = product.Name;
            model.OriginalPrice = product.Price;
            model.HasSale = ProductSaleCalculator.IsActive(activeSale, now);
            model.Price = ProductSaleCalculator.GetEffectivePrice(product.Price, activeSale);
            model.SaleBadgeText = ProductSaleCalculator.GetBadgeText(activeSale);
        }

        if (user != null)
        {
            model.UserName = user.Name;
            model.DefaultPhone = user.PhoneNumber;

            if (string.IsNullOrWhiteSpace(model.PostpaidNationalId))
            {
                model.PostpaidNationalId = user.NationalId;
            }

            if (string.IsNullOrWhiteSpace(model.PostpaidBillingAddress))
            {
                model.PostpaidBillingAddress = user.BillingAddress;
            }

            var options = new List<string>();
            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                options.Add(user.PhoneNumber);
            }

            foreach (var phone in recentPhones)
            {
                if (string.IsNullOrWhiteSpace(phone))
                {
                    continue;
                }

                if (!options.Contains(phone))
                {
                    options.Add(phone);
                }

                if (options.Count >= 2)
                {
                    break;
                }
            }

            model.PhoneOptions = options;

            if (string.IsNullOrWhiteSpace(model.SelectedPhoneOption) &&
                !string.IsNullOrWhiteSpace(model.Phone) &&
                options.Contains(model.Phone))
            {
                model.SelectedPhoneOption = model.Phone;
            }
        }

        model.SavedCards = cards
            .Select(card => new SavedCardOptionViewModel
            {
                Id = card.Id,
                MaskedNumber = Mask(card.CardNumber),
                Expiry = card.ExpiryDate.ToString("MM/yy"),
                Balance = card.Balance,
                Label = $"{Mask(card.CardNumber)} • exp {card.ExpiryDate:MM/yy} • {card.Balance:N0} VND"
            })
            .ToList();

        if (model.SavedCards.Any())
        {
            model.CardInputMode = string.IsNullOrWhiteSpace(model.CardInputMode)
                ? "saved"
                : model.CardInputMode.Trim().ToLowerInvariant();

            if (model.SavedCardId == null)
            {
                model.SavedCardId = model.SavedCards[0].Id;
            }
        }
        else
        {
            model.CardInputMode = "manual";
        }

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

    private sealed class CheckoutDraft
    {
        public int ProductId { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public string? PostpaidNationalId { get; set; }
        public string? PostpaidBillingAddress { get; set; }

        public string? CardNumber { get; set; }
        public string? CVV { get; set; }
        public string? Expiry { get; set; }

        public string? MaskedCardNumber { get; set; }
        public string? CardLabel { get; set; }
    }

    private string SaveDraftToSession(CheckoutDraft draft)
    {
        var token = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString(CheckoutDraftSessionPrefix + token, JsonSerializer.Serialize(draft));
        return token;
    }

    private CheckoutDraft? ReadDraftFromSession(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var raw = HttpContext.Session.GetString(CheckoutDraftSessionPrefix + token);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CheckoutDraft>(raw);
        }
        catch
        {
            return null;
        }
    }

    private void RemoveDraftFromSession(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        HttpContext.Session.Remove(CheckoutDraftSessionPrefix + token);
    }

    public IActionResult Index(int productId)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var selectedPhone = (HttpContext.Session.GetString(SelectedPhoneSessionKey) ?? string.Empty).Trim();
        var user = _context.Users.AsNoTracking().FirstOrDefault(x => x.Id == userId);

        var model = new CheckoutViewModel
        {
            ProductId = productId,
            Type = "prepaid",
            Phone = !string.IsNullOrWhiteSpace(selectedPhone)
                ? selectedPhone
                : (user?.PhoneNumber ?? string.Empty).Trim()
        };

        LoadData(model, userId);

        if (string.IsNullOrWhiteSpace(model.ProductName))
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Review(CheckoutViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        model.Type = (model.Type ?? string.Empty).Trim().ToLowerInvariant();
        model.Phone = (model.Phone ?? string.Empty).Trim();

        string? cardNumber = model.CardNumber;
        string? cvv = model.CVV;
        string? expiry = model.Expiry;

        if (model.Type == "postpaid")
        {
            ModelState.Remove(nameof(model.CardNumber));
            ModelState.Remove(nameof(model.CVV));
            ModelState.Remove(nameof(model.Expiry));
            ModelState.Remove(nameof(model.CardInputMode));
            ModelState.Remove(nameof(model.SavedCardId));
            ModelState.Remove(nameof(model.PostpaidAgreeToTerms));
            ModelState.Remove(nameof(model.PostpaidAgreeToContract));

            model.PostpaidNationalId = (model.PostpaidNationalId ?? string.Empty).Trim();
            model.PostpaidBillingAddress = (model.PostpaidBillingAddress ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(model.PostpaidNationalId))
            {
                ModelState.AddModelError(nameof(model.PostpaidNationalId), "Please enter your National ID / CCCD.");
            }
            else if (!Regex.IsMatch(model.PostpaidNationalId, @"^\d{9}(\d{3})?$"))
            {
                ModelState.AddModelError(nameof(model.PostpaidNationalId), "National ID / CCCD must be 9 or 12 digits.");
            }

            if (string.IsNullOrWhiteSpace(model.PostpaidBillingAddress))
            {
                ModelState.AddModelError(nameof(model.PostpaidBillingAddress), "Please enter your billing address.");
            }
        }
        else if (model.Type == "prepaid")
        {
            ModelState.Remove(nameof(model.PostpaidNationalId));
            ModelState.Remove(nameof(model.PostpaidBillingAddress));
            ModelState.Remove(nameof(model.PostpaidAgreeToTerms));
            ModelState.Remove(nameof(model.PostpaidAgreeToContract));

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

                if (!Regex.IsMatch(model.CardNumber ?? "", @"^\d{16}$"))
                    ModelState.AddModelError(nameof(model.CardNumber), "Card number must contain 16 digits.");

                if (!Regex.IsMatch(model.CVV ?? "", @"^\d{3}$"))
                    ModelState.AddModelError(nameof(model.CVV), "Invalid CVV.");

                if (!Regex.IsMatch(model.Expiry ?? "", @"^(0[1-9]|1[0-2])\/\d{2}$"))
                    ModelState.AddModelError(nameof(model.Expiry), "Invalid MM/YY format.");
            }
            else
            {
                ModelState.AddModelError(nameof(model.CardInputMode), "Invalid card input mode.");
            }
        }
        else
        {
            ModelState.AddModelError(nameof(model.Type), "Invalid payment type.");
        }

        if (!ModelState.IsValid)
        {
            LoadData(model, userId);
            return View("Index", model);
        }

        LoadData(model, userId);

        string? maskedCard = null;
        string? cardLabel = null;

        if (model.Type == "prepaid")
        {
            if (model.CardInputMode == "saved" && model.SavedCardId.HasValue)
            {
                var selectedOption = model.SavedCards.FirstOrDefault(x => x.Id == model.SavedCardId.Value);
                maskedCard = selectedOption?.MaskedNumber;
                cardLabel = selectedOption?.Label;
            }
            else if (!string.IsNullOrWhiteSpace(cardNumber))
            {
                maskedCard = Mask(cardNumber);
                cardLabel = "Manual card";
            }
        }

        var draft = new CheckoutDraft
        {
            ProductId = model.ProductId,
            Phone = model.Phone,
            Type = model.Type,
            PostpaidNationalId = string.IsNullOrWhiteSpace(model.PostpaidNationalId) ? null : model.PostpaidNationalId.Trim(),
            PostpaidBillingAddress = string.IsNullOrWhiteSpace(model.PostpaidBillingAddress) ? null : model.PostpaidBillingAddress.Trim(),
            CardNumber = model.Type == "prepaid" ? cardNumber : null,
            CVV = model.Type == "prepaid" ? cvv : null,
            Expiry = model.Type == "prepaid" ? expiry : null,
            MaskedCardNumber = maskedCard,
            CardLabel = cardLabel
        };

        var token = SaveDraftToSession(draft);

        return View("Review", new CheckoutReviewViewModel
        {
            Token = token,
            Checkout = model,
            MaskedCardNumber = maskedCard,
            CardLabel = cardLabel
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(CheckoutConfirmViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var draft = ReadDraftFromSession(model.Token);
        if (draft == null)
        {
            TempData["ErrorMessage"] = "Checkout session expired. Please try again.";
            return RedirectToAction("Index", "Product");
        }

        if (string.Equals(draft.Type, "postpaid", StringComparison.OrdinalIgnoreCase))
        {
            if (!model.PostpaidAgreeToTerms)
            {
                ModelState.AddModelError(nameof(model.PostpaidAgreeToTerms), "You must agree to the terms & conditions.");
            }

            if (!model.PostpaidAgreeToContract)
            {
                ModelState.AddModelError(nameof(model.PostpaidAgreeToContract), "You must agree to the postpaid purchase contract.");
            }
        }

        if (!ModelState.IsValid)
        {
            var checkoutModel = new CheckoutViewModel
            {
                ProductId = draft.ProductId,
                Phone = draft.Phone,
                Type = draft.Type,
                PostpaidNationalId = draft.PostpaidNationalId,
                PostpaidBillingAddress = draft.PostpaidBillingAddress
            };

            LoadData(checkoutModel, userId);

            return View("Review", new CheckoutReviewViewModel
            {
                Token = model.Token,
                Checkout = checkoutModel,
                MaskedCardNumber = draft.MaskedCardNumber,
                CardLabel = draft.CardLabel
            });
        }

        HttpContext.Session.SetString(SelectedPhoneSessionKey, draft.Phone);

        var result = _service.ProcessPayment(
            draft.ProductId,
            userId,
            draft.Phone,
            draft.Type,
            draft.PostpaidNationalId,
            draft.PostpaidBillingAddress,
            model.PostpaidAgreeToTerms,
            model.PostpaidAgreeToContract,
            draft.CardNumber,
            draft.CVV,
            draft.Expiry
        );

        if (!result.IsSuccess || result.Transaction == null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not process this transaction.";

            var checkoutModel = new CheckoutViewModel
            {
                ProductId = draft.ProductId,
                Phone = draft.Phone,
                Type = draft.Type,
                PostpaidNationalId = draft.PostpaidNationalId,
                PostpaidBillingAddress = draft.PostpaidBillingAddress
            };

            LoadData(checkoutModel, userId);

            return View("Review", new CheckoutReviewViewModel
            {
                Token = model.Token,
                Checkout = checkoutModel,
                MaskedCardNumber = draft.MaskedCardNumber,
                CardLabel = draft.CardLabel
            });
        }

        RemoveDraftFromSession(model.Token);

        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        var product = _context.Products.FirstOrDefault(x => x.Id == draft.ProductId);

        if (user != null &&
            user.Role == "User" &&
            !user.IsDeleted &&
            string.Equals(draft.Type, "postpaid", StringComparison.OrdinalIgnoreCase))
        {
            var nationalId = string.IsNullOrWhiteSpace(draft.PostpaidNationalId) ? null : draft.PostpaidNationalId.Trim();
            var billingAddress = string.IsNullOrWhiteSpace(draft.PostpaidBillingAddress) ? null : draft.PostpaidBillingAddress.Trim();

            if (user.NationalId != nationalId || user.BillingAddress != billingAddress)
            {
                user.NationalId = nationalId;
                user.BillingAddress = billingAddress;
            }
        }

        if (user != null && product != null)
        {
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            await _emailNotificationService.SendTransactionConfirmationAsync(user, result.Transaction, product.Name);
        }

        return RedirectToAction("Success", new { id = result.Transaction.Id });
    }

    public IActionResult Success(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var transaction = _context.Transactions
            .FirstOrDefault(x => x.Id == id && x.UserId == userId);

        if (transaction == null)
            return NotFound();

        return View(transaction);
    }
}
