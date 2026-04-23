using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Online_Mobile_Recharge.Models.Configuration;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Services;

[Route("Payment")]
[UserAuthorize]
public class PaymentController : Controller
{
    private readonly MobileRechargeDbContext _context;
    private readonly TransactionService _transactionService;
    private readonly PayPalService _payPalService;
    private readonly AppUrlOptions _appUrlOptions;

    public PaymentController(
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
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var transaction = _context.Transactions.FirstOrDefault(x => x.Id == id && x.UserId == userId);

        if (transaction == null ||
            (transaction.Type != TransactionType.Prepaid && transaction.Type != TransactionType.Postpaid) ||
            transaction.Status != TransactionStatus.Pending ||
            transaction.PaymentMethod != PaymentMethod.PayPalSandbox)
        {
            return RedirectToAction("Index", "UserTransaction");
        }

        var accessToken = await _payPalService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            TempData["ErrorMessage"] = "PayPal sandbox is not configured correctly (token request failed).";
            return RedirectToAction("Index", "UserTransaction");
        }

        var baseUrl = (_appUrlOptions.PublicBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var returnUrl = $"{baseUrl}/Payment/PayPalReturn?tx={transaction.Id}";
        var cancelUrl = $"{baseUrl}/Payment/PayPalCancel?tx={transaction.Id}";

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
            return RedirectToAction("Index", "UserTransaction");
        }

        transaction.PaymentExternalId = orderId;
        _context.SaveChanges();

        return Redirect(approveUrl);
    }

    [HttpGet("PayPalReturn")]
    public async Task<IActionResult> PayPalReturn(int tx, string? token, string? PayerID, CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var result = await _transactionService.CompletePayPalApprovedAsync(tx, userId, token, PayerID, cancellationToken);

        if (!result.IsSuccess || result.Transaction == null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "PayPal return failed.";
            return RedirectToAction("Index", "UserTransaction");
        }

        if (result.Transaction.Type == TransactionType.Postpaid)
        {
            TempData["SuccessMessage"] = "Your postpaid bill was paid successfully.";
            return RedirectToAction("Index", "UserTransaction");
        }

        return RedirectToAction("Success", "Checkout", new { id = result.Transaction.Id });
    }

    [HttpGet("PayPalCancel")]
    public IActionResult PayPalCancel(int tx)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        _transactionService.CancelPayPalCheckout(tx, userId, "PayPal checkout canceled.");
        TempData["ErrorMessage"] = "Payment canceled.";
        return RedirectToAction("Index", "UserTransaction");
    }
}
