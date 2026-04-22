namespace Online_Mobile_Recharge.Services;

public readonly record struct DemoCardInfo(
    string CardNumber,
    string CVV,
    int ExpiryMonth,
    int ExpiryYear,
    decimal InitialBalance);

public static class DemoCardCatalog
{
    private static readonly Dictionary<string, DemoCardInfo> Cards = new(StringComparer.Ordinal)
    {
        ["6011223344557001"] = new DemoCardInfo("6011223344557001", "321", 12, 2028, 450000m),
        ["6011223344557002"] = new DemoCardInfo("6011223344557002", "654", 11, 2029, 780000m),
        ["6011223344557003"] = new DemoCardInfo("6011223344557003", "147", 10, 2030, 1050000m),
        ["4555666677778001"] = new DemoCardInfo("4555666677778001", "258", 9, 2029, 1400000m),
        ["4555666677778002"] = new DemoCardInfo("4555666677778002", "369", 8, 2030, 1850000m)
    };

    public static bool TryGet(string? cardNumber, out DemoCardInfo info)
    {
        info = default;
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return false;
        }

        return Cards.TryGetValue(cardNumber.Trim(), out info);
    }
}
