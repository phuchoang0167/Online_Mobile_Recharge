using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Online_Mobile_Recharge.Models.Configuration;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;

namespace Online_Mobile_Recharge.Controllers
{
    public class AccountController : Controller
    {
        private readonly MobileRechargeDbContext _context;
        private readonly EmailNotificationService _emailNotificationService;
        private readonly AppUrlOptions _appUrlOptions;

        public AccountController(
            MobileRechargeDbContext context,
            EmailNotificationService emailNotificationService,
            IOptions<AppUrlOptions> appUrlOptions)
        {
            _context = context;
            _emailNotificationService = emailNotificationService;
            _appUrlOptions = appUrlOptions.Value;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var now = DateTime.Now;
            var normalizedEmail = PasswordHelper.NormalizeEmail(model.Email);
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Incorrect email or password.");
                return View(model);
            }

            if (user.Role == "Guest")
            {
                ModelState.AddModelError(string.Empty, "Guest checkout accounts cannot be used to login. Please register a normal account.");
                return View(model);
            }

            if (user.LoginLockoutUntil != null && user.LoginLockoutUntil.Value > now)
            {
                var unlockAt = user.LoginLockoutUntil.Value.ToString("dd/MM/yyyy HH:mm");
                ModelState.AddModelError(string.Empty, $"Too many failed login attempts. Your account is temporarily locked until {unlockAt}.");
                return View(model);
            }

            if (user.LoginLockoutUntil != null && user.LoginLockoutUntil.Value <= now)
            {
                user.LoginLockoutUntil = null;
            }

            if (user.FailedLoginDate == null || user.FailedLoginDate.Value.Date != now.Date)
            {
                user.FailedLoginDate = now.Date;
                user.FailedLoginAttempts = 0;
            }

            if (user.Role == "User")
            {
                const int postpaidLockGraceDays = 3;
                var lockThreshold = now.AddDays(-postpaidLockGraceDays);
                var hasOverduePostpaid = await _context.Transactions
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.UserId == user.Id &&
                        x.Type == TransactionType.Postpaid &&
                        x.Status == TransactionStatus.Pending &&
                        !x.IsPaid &&
                        x.DueDate != null &&
                        x.DueDate < lockThreshold);

                if (hasOverduePostpaid)
                {
                    user.IsActive = false;
                    user.LastLockedAt = now;
                    user.LastLockReason = "Overdue postpaid bill (more than 3 days past due date)";
                    await _context.SaveChangesAsync();
                }
            }

            if (!user.IsActive)
            {
                var lockReason = (user.LastLockReason ?? string.Empty).Trim();
                var message = string.IsNullOrWhiteSpace(lockReason)
                    ? "Your account is currently locked."
                    : $"Your account is currently locked. Reason: {lockReason}.";
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            if (!user.EmailVerified)
            {
                ViewBag.PendingVerificationEmail = user.Email;
                ModelState.AddModelError(string.Empty, "Your email has not been verified yet. Please check your inbox or resend the verification email.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError(string.Empty, "This account does not have a password yet. Please reset your password.");
                return View(model);
            }

            var passwordCheck = PasswordHelper.VerifyPassword(user, model.Password);
            if (!passwordCheck.IsValid)
            {
                user.FailedLoginAttempts += 1;
                user.FailedLoginDate = now.Date;

                if (user.FailedLoginAttempts >= 3)
                {
                    user.LoginLockoutUntil = now.Date.AddDays(1);
                    user.LastLockedAt = now;
                    user.LastLockReason = "Too many failed login attempts";
                }

                await _context.SaveChangesAsync();

                if (user.LoginLockoutUntil != null && user.LoginLockoutUntil.Value > now)
                {
                    var unlockAt = user.LoginLockoutUntil.Value.ToString("dd/MM/yyyy HH:mm");
                    ModelState.AddModelError(string.Empty, $"Too many failed login attempts. Your account is temporarily locked until {unlockAt}.");
                    return View(model);
                }

                var remainingAttempts = Math.Max(0, 3 - user.FailedLoginAttempts);
                ModelState.AddModelError(string.Empty, $"Incorrect email or password. Remaining attempts today: {remainingAttempts}.");
                return View(model);
            }

            if (passwordCheck.ShouldUpgrade)
            {
                user.Password = PasswordHelper.HashPassword(user, model.Password);
            }

            user.FailedLoginAttempts = 0;
            user.FailedLoginDate = now.Date;
            user.LoginLockoutUntil = null;
            await _context.SaveChangesAsync();

            await SignInUserAsync(user);
            return RedirectAfterLogin(user, returnUrl);
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = PasswordHelper.NormalizeEmail(model.Email);
            var exists = await _context.Users.AnyAsync(x => x.Email == normalizedEmail);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                return View(model);
            }

            var user = new User
            {
                Name = model.Name.Trim(),
                Email = normalizedEmail,
                Password = string.Empty,
                PhoneNumber = model.PhoneNumber.Trim(),
                Role = "User",
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };
            user.Password = PasswordHelper.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var verificationResult = await IssueEmailVerificationAsync(user);

            TempData["SuccessMessage"] = "Registration successful. Please verify your email before signing in.";
            StoreFallbackLink(verificationResult.Url, verificationResult.EmailSent, "verification");

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = PasswordHelper.NormalizeEmail(model.Email);
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted && x.IsActive);

            if (user != null)
            {
                var resetResult = await IssuePasswordResetAsync(user);
                StoreFallbackLink(resetResult.Url, resetResult.EmailSent, "password reset");
            }

            TempData["SuccessMessage"] = "If the email exists in our system, we have sent a password reset link.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirm(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["AuthError"] = "The password reset link is invalid.";
                return RedirectToAction(nameof(ResetPassword));
            }

            return View(new ResetPasswordConfirmViewModel
            {
                Email = email,
                Token = token
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordConfirm(ResetPasswordConfirmViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = PasswordHelper.NormalizeEmail(model.Email);
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted);

            if (user == null || !IsTokenValid(user.PasswordResetTokenHash, user.PasswordResetTokenExpiresAt, model.Token))
            {
                ModelState.AddModelError(string.Empty, "The password reset link is invalid or has expired.");
                return View(model);
            }

            if (PasswordHelper.VerifyPassword(user, model.NewPassword).IsValid)
            {
                ModelState.AddModelError(nameof(model.NewPassword), "The new password must be different from your current password.");
                return View(model);
            }

            user.Password = PasswordHelper.HashPassword(user, model.NewPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            user.EmailVerified = true;
            user.EmailVerificationTokenHash = null;
            user.EmailVerificationTokenExpiresAt = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been updated. You can sign in now.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string email, string token)
        {
            var normalizedEmail = PasswordHelper.NormalizeEmail(email);
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted);

            if (user == null || !IsTokenValid(user.EmailVerificationTokenHash, user.EmailVerificationTokenExpiresAt, token))
            {
                TempData["AuthError"] = "The email verification link is invalid or has expired.";
                return RedirectToAction(nameof(Login));
            }

            user.EmailVerified = true;
            user.EmailVerificationTokenHash = null;
            user.EmailVerificationTokenExpiresAt = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your email has been verified successfully.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail(string email, string? returnUrl = null)
        {
            var normalizedEmail = PasswordHelper.NormalizeEmail(email);
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted && x.IsActive);

            if (user != null && !user.EmailVerified)
            {
                var verificationResult = await IssueEmailVerificationAsync(user);
                StoreFallbackLink(verificationResult.Url, verificationResult.EmailSent, "verification");
            }

            TempData["SuccessMessage"] = "If the account is still unverified, we have resent the verification email.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private async Task<(string Url, bool EmailSent)> IssueEmailVerificationAsync(User user)
        {
            var rawToken = AuthTokenHelper.GenerateToken();
            user.EmailVerificationTokenHash = AuthTokenHelper.HashToken(rawToken);
            user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            var verificationUrl = BuildPublicUrl(
                nameof(VerifyEmail),
                new { email = user.Email, token = rawToken });

            var emailSent = await _emailNotificationService.SendEmailVerificationAsync(user, verificationUrl);
            return (verificationUrl, emailSent);
        }

        private async Task<(string Url, bool EmailSent)> IssuePasswordResetAsync(User user)
        {
            var rawToken = AuthTokenHelper.GenerateToken();
            user.PasswordResetTokenHash = AuthTokenHelper.HashToken(rawToken);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
            await _context.SaveChangesAsync();

            var resetUrl = BuildPublicUrl(
                nameof(ResetPasswordConfirm),
                new { email = user.Email, token = rawToken });

            var emailSent = await _emailNotificationService.SendPasswordResetAsync(user, resetUrl);
            return (resetUrl, emailSent);
        }

        private string BuildPublicUrl(string actionName, object routeValues)
        {
            var publicBaseUrl = (_appUrlOptions.PublicBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                var relativePath = Url.Action(actionName, "Account", routeValues) ?? string.Empty;
                return string.IsNullOrWhiteSpace(relativePath)
                    ? publicBaseUrl
                    : $"{publicBaseUrl}{relativePath}";
            }

            return Url.Action(actionName, "Account", routeValues, Request.Scheme) ?? string.Empty;
        }

        private void StoreFallbackLink(string url, bool emailSent, string linkType)
        {
            if (emailSent || string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            TempData["DebugLink"] = url;
            TempData["DebugLinkLabel"] = linkType;
        }

        private static bool IsTokenValid(string? storedHash, DateTime? expiresAt, string rawToken)
        {
            if (string.IsNullOrWhiteSpace(storedHash) ||
                expiresAt == null ||
                expiresAt < DateTime.UtcNow ||
                string.IsNullOrWhiteSpace(rawToken))
            {
                return false;
            }

            return string.Equals(storedHash, AuthTokenHelper.HashToken(rawToken), StringComparison.Ordinal);
        }

        private async Task SignInUserAsync(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Name", user.Name);
            HttpContext.Session.SetString("Role", user.Role);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true
                });
        }

        private IActionResult RedirectAfterLogin(User user, string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return user.Role == "Admin"
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Index", "Home");
        }
    }
}
