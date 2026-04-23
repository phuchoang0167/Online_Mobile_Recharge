using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Online_Mobile_Recharge.Models.Configuration;

namespace Online_Mobile_Recharge.Services;

public class PayPalService
{
    private readonly HttpClient _httpClient;
    private readonly PayPalOptions _options;

    public PayPalService(HttpClient httpClient, IOptions<PayPalOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    private string BaseUrl =>
        string.Equals(_options.Environment, "Live", StringComparison.OrdinalIgnoreCase)
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/oauth2/token");
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.Secret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return doc.RootElement.TryGetProperty("access_token", out var token) ? token.GetString() : null;
    }

    public async Task<(string? OrderId, string? ApproveUrl)> CreateOrderAsync(
        string accessToken,
        decimal amount,
        string currency,
        string returnUrl,
        string cancelUrl,
        string referenceId,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = referenceId,
                    amount = new
                    {
                        currency_code = currency,
                        value = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (null, null);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var orderId = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        string? approveUrl = null;

        if (doc.RootElement.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Array)
        {
            foreach (var link in links.EnumerateArray())
            {
                var rel = link.TryGetProperty("rel", out var relEl) ? relEl.GetString() : null;
                if (!string.Equals(rel, "approve", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                approveUrl = link.TryGetProperty("href", out var hrefEl) ? hrefEl.GetString() : null;
                break;
            }
        }

        return (orderId, approveUrl);
    }

    public async Task<bool> CaptureOrderAsync(string accessToken, string orderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return false;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/checkout/orders/{orderId}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var status = doc.RootElement.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        return string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<(string? Status, string? Currency, decimal? Amount)> GetOrderSummaryAsync(
        string accessToken,
        string orderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return (null, null, null);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v2/checkout/orders/{orderId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (null, null, null);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var status = doc.RootElement.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;

        try
        {
            if (doc.RootElement.TryGetProperty("purchase_units", out var purchaseUnits) &&
                purchaseUnits.ValueKind == JsonValueKind.Array &&
                purchaseUnits.GetArrayLength() > 0)
            {
                var firstUnit = purchaseUnits[0];
                if (firstUnit.TryGetProperty("amount", out var amountEl))
                {
                    var currency = amountEl.TryGetProperty("currency_code", out var ccEl) ? ccEl.GetString() : null;
                    var value = amountEl.TryGetProperty("value", out var valueEl) ? valueEl.GetString() : null;

                    if (decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    {
                        return (status, currency, parsed);
                    }

                    return (status, currency, null);
                }
            }
        }
        catch
        {
            // ignore parse errors and return whatever we have
        }

        return (status, null, null);
    }
}
