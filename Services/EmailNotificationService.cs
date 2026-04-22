using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Online_Mobile_Recharge.Models.Configuration;
using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Services;

public class EmailNotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly SmtpOptions _smtpOptions;

    public EmailNotificationService(
        ILogger<EmailNotificationService> logger,
        IOptions<SmtpOptions> smtpOptions)
    {
        _logger = logger;
        _smtpOptions = smtpOptions.Value;
    }

    public bool IsConfigured => _smtpOptions.IsConfigured;

    public async Task<bool> SendContactConfirmationAsync(string name, string email, string messageType)
    {
        var subject = $"We received your {messageType}";
        var body = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6;color:#0f172a;">
                <h2 style="margin-bottom:12px;">Hello {WebUtility.HtmlEncode(name)},</h2>
                <p>We have received your {WebUtility.HtmlEncode(messageType)}.</p>
                <p>Our support team will review it and reply by email as soon as possible.</p>
                <p style="margin-top:24px;">Online Mobile Recharge</p>
            </div>
            """;

        return await SendAsync(email, name, subject, body);
    }

    public async Task<bool> SendSupportInboxNotificationAsync(string name, string email, string message, string source)
    {
        var supportInbox = string.IsNullOrWhiteSpace(_smtpOptions.SupportEmail)
            ? _smtpOptions.FromEmail
            : _smtpOptions.SupportEmail;

        if (string.IsNullOrWhiteSpace(supportInbox))
        {
            _logger.LogInformation("Support inbox is not configured, skipped support notification for {Email}.", email);
            return false;
        }

        var subject = $"New {source} from {name}";
        var body = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6;color:#0f172a;">
                <h2 style="margin-bottom:12px;">New {WebUtility.HtmlEncode(source)}</h2>
                <p><strong>Name:</strong> {WebUtility.HtmlEncode(name)}</p>
                <p><strong>Email:</strong> {WebUtility.HtmlEncode(email)}</p>
                <p><strong>Message:</strong></p>
                <div style="padding:14px 16px;border-radius:14px;background:#f8fafc;border:1px solid #e2e8f0;">
                    {WebUtility.HtmlEncode(message).Replace(Environment.NewLine, "<br />")}
                </div>
            </div>
            """;

        return await SendAsync(
            supportInbox,
            "Support Team",
            subject,
            body,
            replyToEmail: email,
            replyToName: name);
    }

    public async Task<bool> SendTransactionConfirmationAsync(User user, Transaction transaction, string productName)
    {
        var title = transaction.Status switch
        {
            Models.Enums.TransactionStatus.Success => "Payment successful",
            Models.Enums.TransactionStatus.Pending => "Postpaid bill created",
            _ => "Transaction update"
        };

        var dueDateMarkup = transaction.DueDate == null
            ? string.Empty
            : $"<p><strong>Due date:</strong> {transaction.DueDate:dd/MM/yyyy HH:mm}</p>";

        var postpaidReminderMarkup = transaction.Type == Models.Enums.TransactionType.Postpaid &&
                                     transaction.Status == Models.Enums.TransactionStatus.Pending
            ? "<p style=\"margin-top:16px;\"><strong>Reminder:</strong> Please settle this postpaid bill before the due date. Overdue bills may lock your account.</p>"
            : string.Empty;

        var body = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6;color:#0f172a;">
                <h2 style="margin-bottom:12px;">{title}</h2>
                <p>Hello {WebUtility.HtmlEncode(user.Name)}, your transaction has been updated.</p>
                <p><strong>Product:</strong> {WebUtility.HtmlEncode(productName)}</p>
                <p><strong>Phone number:</strong> {WebUtility.HtmlEncode(transaction.PhoneNumber)}</p>
                <p><strong>Amount:</strong> {transaction.Amount:N0} VND</p>
                <p><strong>Type:</strong> {transaction.Type}</p>
                <p><strong>Status:</strong> {transaction.Status}</p>
                {dueDateMarkup}
                {postpaidReminderMarkup}
                <p style="margin-top:24px;">You can open your dashboard to view details and transaction history.</p>
            </div>
            """;

        return await SendAsync(user.Email, user.Name, $"Recharge update #{transaction.Id}", body);
    }

    public async Task<bool> SendEmailVerificationAsync(User user, string verificationUrl)
    {
        var body = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6;color:#0f172a;">
                <h2 style="margin-bottom:12px;">Verify your email</h2>
                <p>Hello {WebUtility.HtmlEncode(user.Name)}, please verify your email to fully activate your account.</p>
                <p><a href="{WebUtility.HtmlEncode(verificationUrl)}" style="display:inline-block;padding:12px 18px;border-radius:999px;background:#0f766e;color:#ffffff;text-decoration:none;font-weight:700;">Verify email</a></p>
                <p>This link will expire in 24 hours.</p>
            </div>
            """;

        return await SendAsync(user.Email, user.Name, "Verify your email", body);
    }

    public async Task<bool> SendPasswordResetAsync(User user, string resetUrl)
    {
        var body = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6;color:#0f172a;">
                <h2 style="margin-bottom:12px;">Reset your password</h2>
                <p>Hello {WebUtility.HtmlEncode(user.Name)}, we received a request to reset your account password.</p>
                <p><a href="{WebUtility.HtmlEncode(resetUrl)}" style="display:inline-block;padding:12px 18px;border-radius:999px;background:#0f766e;color:#ffffff;text-decoration:none;font-weight:700;">Create a new password</a></p>
                <p>This link will expire in 30 minutes.</p>
            </div>
            """;

        return await SendAsync(user.Email, user.Name, "Reset your password", body);
    }

    public async Task<bool> SendPostpaidReminderAsync(User user, Transaction transaction, string productName)
    {
        var dueDateMarkup = transaction.DueDate == null
            ? string.Empty
            : $"<p><strong>Due date:</strong> {transaction.DueDate:dd/MM/yyyy HH:mm}</p>";

        var body = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6;color:#0f172a;">
                <h2 style="margin-bottom:12px;">Postpaid bill reminder</h2>
                <p>Hello {WebUtility.HtmlEncode(user.Name)}, here is a reminder about your postpaid bill.</p>
                <p><strong>Product:</strong> {WebUtility.HtmlEncode(productName)}</p>
                <p><strong>Phone number:</strong> {WebUtility.HtmlEncode(transaction.PhoneNumber)}</p>
                <p><strong>Amount:</strong> {transaction.Amount:N0} VND</p>
                {dueDateMarkup}
                <p>You can open your dashboard and pay this bill by entering the card details manually.</p>
            </div>
            """;

        return await SendAsync(user.Email, user.Name, $"Postpaid reminder #{transaction.Id}", body);
    }

    public async Task<bool> SendProductChangeNoticeAsync(User user, string oldProductName, string newProductName, string changeSummary)
    {
        var body = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6;color:#0f172a;">
                <h2 style="margin-bottom:12px;">Package update notice</h2>
                <p>Hello {WebUtility.HtmlEncode(user.Name)}, we updated one of the packages on our platform.</p>
                <p><strong>Old package:</strong> {WebUtility.HtmlEncode(oldProductName)}</p>
                <p><strong>New package:</strong> {WebUtility.HtmlEncode(newProductName)}</p>
                <div style="margin-top:12px;padding:14px 16px;border-radius:14px;background:#f8fafc;border:1px solid #e2e8f0;">
                    {WebUtility.HtmlEncode(changeSummary).Replace(Environment.NewLine, "<br />")}
                </div>
                <p style="margin-top:24px;">If you are currently using this package, your existing benefits remain until expiry (when applicable).</p>
            </div>
            """;

        return await SendAsync(user.Email, user.Name, "Package updated", body);
    }

    private async Task<bool> SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        string? replyToEmail = null,
        string? replyToName = null)
    {
        if (!IsConfigured)
        {
            _logger.LogInformation("SMTP is not configured, skipped email to {Email}.", toEmail);
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpOptions.FromName, _smtpOptions.FromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            if (!string.IsNullOrWhiteSpace(replyToEmail))
            {
                message.ReplyTo.Add(new MailboxAddress(
                    string.IsNullOrWhiteSpace(replyToName) ? replyToEmail : replyToName,
                    replyToEmail));
            }

            message.Body = new BodyBuilder
            {
                HtmlBody = htmlBody
            }.ToMessageBody();

            using var client = new SmtpClient();
            var socketOptions = ResolveSocketOptions(_smtpOptions.Port, _smtpOptions.EnableSsl);
            var userName = (_smtpOptions.UserName ?? string.Empty).Trim();
            var password = (_smtpOptions.Password ?? string.Empty).Trim();

            await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, socketOptions);
            await client.AuthenticateAsync(userName, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send email to {Email}.", toEmail);
            return false;
        }
    }

    private static SecureSocketOptions ResolveSocketOptions(int port, bool enableSsl)
    {
        if (!enableSsl)
        {
            return SecureSocketOptions.None;
        }

        if (port == 587)
        {
            return SecureSocketOptions.StartTls;
        }

        return port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;
    }
}
