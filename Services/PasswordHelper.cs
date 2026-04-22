using Microsoft.AspNetCore.Identity;
using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Services;

public static class PasswordHelper
{
    private static readonly PasswordHasher<User> Hasher = new();

    public static string HashPassword(User user, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return Hasher.HashPassword(user, password);
    }

    public static PasswordCheckResult VerifyPassword(User user, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(user.Password) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return PasswordCheckResult.Failed();
        }

        try
        {
            var result = Hasher.VerifyHashedPassword(user, user.Password, providedPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                return VerifyLegacyPassword(user, providedPassword);
            }

            return PasswordCheckResult.Success(result == PasswordVerificationResult.SuccessRehashNeeded);
        }
        catch (FormatException)
        {
            return VerifyLegacyPassword(user, providedPassword);
        }
    }

    public static string NormalizeEmail(string email) =>
        string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToLowerInvariant();

    private static PasswordCheckResult VerifyLegacyPassword(User user, string providedPassword) =>
        string.Equals(user.Password, providedPassword, StringComparison.Ordinal)
            ? PasswordCheckResult.Success(shouldUpgrade: true)
            : PasswordCheckResult.Failed();
}

public readonly record struct PasswordCheckResult(bool IsValid, bool ShouldUpgrade)
{
    public static PasswordCheckResult Success(bool shouldUpgrade = false) => new(true, shouldUpgrade);

    public static PasswordCheckResult Failed() => new(false, false);
}
