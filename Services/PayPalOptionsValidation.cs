using Microsoft.Extensions.Options;
using Online_Mobile_Recharge.Models.Configuration;

namespace Online_Mobile_Recharge.Services;

public sealed class PayPalOptionsValidation : IValidateOptions<PayPalOptions>
{
    public ValidateOptionsResult Validate(string? name, PayPalOptions options)
    {
        var env = (options.Environment ?? "Sandbox").Trim();
        if (!string.Equals(env, "Sandbox", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(env, "Live", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail("PayPal:Environment must be 'Sandbox' or 'Live'.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            return ValidateOptionsResult.Fail("PayPal:ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            return ValidateOptionsResult.Fail("PayPal:Secret is required.");
        }

        if (string.Equals(options.ClientId.Trim(), options.Secret.Trim(), StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("PayPal:Secret must NOT equal PayPal:ClientId (use the Client Secret from PayPal Developer dashboard).");
        }

        return ValidateOptionsResult.Success;
    }
}

