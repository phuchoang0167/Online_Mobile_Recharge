using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Services;
using System.Text.RegularExpressions;

namespace Online_Mobile_Recharge.Controllers;

public class GuestCheckoutController : Controller
{
    private const string SelectedPhoneSessionKey = "SelectedPhoneNumber";

    private readonly MobileRechargeDbContext _context;
    private readonly TransactionService _service;

    public GuestCheckoutController(MobileRechargeDbContext context, TransactionService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public IActionResult Index(int productId)
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "Checkout", new { productId });
        }

        var product = _context.Products.AsNoTracking().FirstOrDefault(x => x.Id == productId);
        if (product == null)
        {
            return NotFound();
        }

        if (product.Type != ProductType.Card)
        {
            TempData["ErrorMessage"] = "Guest users can only checkout topup products.";
            return RedirectToAction("Index", "Product");
        }

        var phone = (HttpContext.Session.GetString(SelectedPhoneSessionKey) ?? string.Empty).Trim();
        ViewBag.Phone = phone;
        ViewBag.Product = product;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(
        int productId,
        string? phone,
        string? paymentMethod,
        string? cardNumber,
        string? cvv,
        string? expiry)
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "Checkout", new { productId });
        }

        var product = _context.Products.AsNoTracking().FirstOrDefault(x => x.Id == productId);
        if (product == null)
        {
            return NotFound();
        }

        if (product.Type != ProductType.Card)
        {
            TempData["ErrorMessage"] = "Guest users can only checkout topup products.";
            return RedirectToAction("Index", "Product");
        }

        phone = (phone ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^0\d{9}$"))
        {
            TempData["ErrorMessage"] = "Please enter a valid phone number (0xxxxxxxxx) first.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        HttpContext.Session.SetString(SelectedPhoneSessionKey, phone);
        var guestUser = await GetOrCreateGuestUserAsync(phone);

        var normalizedMethod = (paymentMethod ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedMethod))
        {
            normalizedMethod = "card";
        }

        if (normalizedMethod is not ("card" or "paypal"))
        {
            TempData["ErrorMessage"] = "Invalid payment method.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        var result = _service.ProcessPayment(
            productId,
            guestUser.Id,
            phone,
            type: "prepaid",
            prepaidPaymentMethod: normalizedMethod,
            postpaidNationalId: null,
            postpaidBillingAddress: null,
            postpaidAgreeToTerms: false,
            postpaidAgreeToContract: false,
            cardNumber: normalizedMethod == "card" ? cardNumber : null,
            cvv: normalizedMethod == "card" ? cvv : null,
            expiry: normalizedMethod == "card" ? expiry : null);

        if (!result.IsSuccess || result.Transaction == null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not process this payment.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        if (result.Transaction.Status == TransactionStatus.Pending &&
            result.Transaction.PaymentMethod == PaymentMethod.PayPalSandbox)
        {
            return RedirectToAction("PayPal", "GuestPayment", new { id = result.Transaction.Id });
        }

        TempData["SuccessMessage"] = "Topup payment completed successfully.";
        return RedirectToAction(nameof(Success), new { id = result.Transaction.Id });
    }

    [HttpGet]
    public IActionResult Success(int id)
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Success", "Checkout", new { id });
        }

        var transaction = _context.Transactions
            .Include(x => x.Product)
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == id && x.User.Role == "Guest");

        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    private async Task<User> GetOrCreateGuestUserAsync(string phone)
    {
        var normalizedPhone = Regex.Replace((phone ?? string.Empty).Trim(), @"[^\d]", string.Empty);
        var email = $"guest+{normalizedPhone}@guest.local";

        var existing = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existing != null)
        {
            if (existing.Role != "Guest")
            {
                existing.Role = "Guest";
                await _context.SaveChangesAsync();
            }

            return existing;
        }

        var user = new User
        {
            Name = $"Guest {normalizedPhone}",
            Email = email,
            Role = "Guest",
            PhoneNumber = normalizedPhone,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        // Prevent login: random password + role not accepted by UserAuthorize.
        user.Password = PasswordHelper.HashPassword(user, Guid.NewGuid().ToString("N"));

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
