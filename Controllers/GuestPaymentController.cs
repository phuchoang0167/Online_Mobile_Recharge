using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Online_Mobile_Recharge.Models.Configuration;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Services;

namespace Online_Mobile_Recharge.Controllers;

[Route("GuestPayment")]
public class GuestPaymentController : Controller
{
    private const string SelectedPhoneSessionKey = "SelectedPhoneNumber";

    private readonly MobileRechargeDbContext _context;
    private readonly TransactionService _transactionService;
    private readonly PayPalService _payPalService;
    private readonly AppUrlOptions _appUrlOptions;

    public GuestPaymentController(
        MobileRechargeDbContext context,
        TransactionService transactionService,
        PayPalService payPalService,
        IOptions<AppUrlOptions> appUrlOptions)
    {
        _context = context;
        _transactionService = transactionService;
        _payPalService = payPalService;
        _appUrlOptions = appUrlOptions.Value;
    }

    [HttpGet("PayPal/{id:int}")]
    public async Task<IActionResult> PayPal(int id, CancellationToken cancellationToken)
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "UserTransaction");
        }

        var phone = (HttpContext.Session.GetString(SelectedPhoneSessionKey) ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["ErrorMessage"] = "Guest session expired. Please start checkout again.";
            return RedirectToAction("Index", "Product");
        }

        var transaction = _context.Transactions
            .Include(x => x.User)
            .FirstOrDefault(x =>
                x.Id == id &&
                x.Type == TransactionType.Prepaid &&
                x.Status == TransactionStatus.Pending &&
                x.PaymentMethod == PaymentMethod.PayPalSandbox &&
                x.User != null &&
                x.User.Role == "Guest" &&
                x.PhoneNumber == phone);

        if (transaction == null)
        {
            TempData["ErrorMessage"] = "Could not find a pending guest PayPal transaction.";
            return RedirectToAction("Index", "Product");
        }

        var accessToken = await _payPalService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            TempData["ErrorMessage"] = "PayPal sandbox is not configured correctly (token request failed).";
            return RedirectToAction("Index", "Product");
        }

        var baseUrl = (_appUrlOptions.PublicBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var returnUrl = $"{baseUrl}/GuestPayment/PayPalReturn?tx={transaction.Id}";
        var cancelUrl = $"{baseUrl}/GuestPayment/PayPalCancel?tx={transaction.Id}";

        var (orderId, approveUrl) = await _payPalService.CreateOrderAsync(
            accessToken,
            transaction.Amount,
            "USD",
            returnUrl,
            cancelUrl,
            referenceId: $"TX{transaction.Id}",
            cancellationToken);

        if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(approveUrl))
        {
            TempData["ErrorMessage"] = "Could not create PayPal order.";
            return RedirectToAction("Index", "Product");
        }

        transaction.PaymentExternalId = orderId;
        _context.SaveChanges();

        return Redirect(approveUrl);
    }

    [HttpGet("PayPalReturn")]
    public async Task<IActionResult> PayPalReturn(int tx, string? token, string? PayerID, CancellationToken cancellationToken)
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "UserTransaction");
        }

        var phone = (HttpContext.Session.GetString(SelectedPhoneSessionKey) ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["ErrorMessage"] = "Guest session expired. Please start checkout again.";
            return RedirectToAction("Index", "Product");
        }

        var transaction = _context.Transactions
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefault(x =>
                x.Id == tx &&
                x.Type == TransactionType.Prepaid &&
                x.PaymentMethod == PaymentMethod.PayPalSandbox &&
                x.User != null &&
                x.User.Role == "Guest" &&
                x.PhoneNumber == phone);

        if (transaction == null)
        {
            TempData["ErrorMessage"] = "PayPal return failed (transaction not found).";
            return RedirectToAction("Index", "Product");
        }

        var result = await _transactionService.CompletePayPalApprovedAsync(
            tx,
            transaction.UserId,
            token,
            PayerID,
            cancellationToken);

        if (!result.IsSuccess || result.Transaction == null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "PayPal return failed.";
            return RedirectToAction("Index", "Product");
        }

        TempData["SuccessMessage"] = "Topup payment completed successfully.";
        return RedirectToAction("Success", "GuestCheckout", new { id = result.Transaction.Id });
    }

    [HttpGet("PayPalCancel")]
    public IActionResult PayPalCancel(int tx)
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "UserTransaction");
        }

        var phone = (HttpContext.Session.GetString(SelectedPhoneSessionKey) ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["ErrorMessage"] = "Payment canceled.";
            return RedirectToAction("Index", "Product");
        }

        var transaction = _context.Transactions
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefault(x =>
                x.Id == tx &&
                x.Type == TransactionType.Prepaid &&
                x.PaymentMethod == PaymentMethod.PayPalSandbox &&
                x.User != null &&
                x.User.Role == "Guest" &&
                x.PhoneNumber == phone);

        if (transaction != null)
        {
            _transactionService.CancelPayPalCheckout(tx, transaction.UserId, "PayPal checkout canceled.");
        }

        TempData["ErrorMessage"] = "Payment canceled.";
        return RedirectToAction("Index", "Product");
    }
}

