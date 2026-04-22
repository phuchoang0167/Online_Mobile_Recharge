using Microsoft.AspNetCore.Mvc;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;

[Route("User/Cards")]
[UserAuthorize]
public class UserCardController : Controller
{
    private readonly MobileRechargeDbContext _context;

    public UserCardController(MobileRechargeDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public IActionResult Index(int? editId = null)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        return View(BuildViewModel(userId, editId));
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public IActionResult Save(CardFormViewModel form)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        if (!TryParseExpiry(form.Expiry, out var expiryDate))
        {
            ModelState.AddModelError(nameof(form.Expiry), "Invalid expiry date.");
        }
        else if (expiryDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(form.Expiry), "The card has expired.");
        }

        var cardNumber = (form.CardNumber ?? string.Empty).Trim();

        var duplicateExists = _context.Cards.Any(x =>
            x.UserId == userId &&
            x.CardNumber == cardNumber &&
            x.Id != form.Id.GetValueOrDefault());
        if (duplicateExists)
        {
            ModelState.AddModelError(nameof(form.CardNumber), "This card number is already saved in your wallet.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", BuildViewModel(userId, form.Id, form));
        }

        if (!DemoCardCatalog.TryGet(cardNumber, out var demoCard))
        {
            ModelState.AddModelError(nameof(form.CardNumber), "This card number is not in the demo list. Please use the sample cards from doc/DEMO_DATA.md");
            return View("Index", BuildViewModel(userId, form.Id, form));
        }

        var cvv = (form.CVV ?? string.Empty).Trim();
        if (!string.Equals(cvv, demoCard.CVV, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(form.CVV), "The CVV does not match this card.");
            return View("Index", BuildViewModel(userId, form.Id, form));
        }

        if (expiryDate.Month != demoCard.ExpiryMonth || expiryDate.Year != demoCard.ExpiryYear)
        {
            ModelState.AddModelError(nameof(form.Expiry), "The expiry date does not match this card.");
            return View("Index", BuildViewModel(userId, form.Id, form));
        }

        Card card;
        if (form.Id.HasValue)
        {
            card = _context.Cards.FirstOrDefault(x => x.Id == form.Id.Value && x.UserId == userId)!;
            if (card == null)
            {
                return NotFound();
            }
        }
        else
        {
            card = new Card
            {
                UserId = userId,
                Price = 0,
                Balance = demoCard.InitialBalance
            };
            _context.Cards.Add(card);
        }

        var wasDifferentNumber = form.Id.HasValue && !string.Equals(card.CardNumber, cardNumber, StringComparison.Ordinal);

        card.CardNumber = cardNumber;
        card.CVV = demoCard.CVV;
        card.ExpiryDate = expiryDate;

        TempData["SuccessMessage"] = form.Id.HasValue
            ? "Card details updated successfully."
            : "Card saved successfully. You can now use it in checkout.";

        if (wasDifferentNumber)
        {
            TempData["InfoMessage"] = "Balance is not reset when updating card details. If you want a fresh balance, delete the card and add it again from doc/DEMO_DATA.md.";
        }

        _context.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var card = _context.Cards.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (card == null)
        {
            return NotFound();
        }

        _context.Cards.Remove(card);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Card removed successfully.";
        return RedirectToAction(nameof(Index));
    }

    private ManageCardsViewModel BuildViewModel(int userId, int? editId, CardFormViewModel? form = null)
    {
        var model = new ManageCardsViewModel
        {
            Cards = _context.Cards
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Balance)
                .ThenBy(x => x.ExpiryDate)
                .ToList(),
            Form = form ?? new CardFormViewModel()
        };

        if (form != null || !editId.HasValue)
        {
            return model;
        }

        var card = model.Cards.FirstOrDefault(x => x.Id == editId.Value);
        if (card == null)
        {
            return model;
        }

        model.Form = new CardFormViewModel
        {
            Id = card.Id,
            CardNumber = card.CardNumber,
            CVV = card.CVV,
            Expiry = card.ExpiryDate.ToString("MM/yy")
        };

        return model;
    }

    private static bool TryParseExpiry(string? expiry, out DateTime expiryDate)
    {
        expiryDate = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(expiry))
        {
            return false;
        }

        var parts = expiry.Split('/');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var month) ||
            !int.TryParse(parts[1], out var year) ||
            month is < 1 or > 12)
        {
            return false;
        }

        expiryDate = new DateTime(2000 + year, month, 1).AddMonths(1).AddDays(-1);
        return true;
    }
}
