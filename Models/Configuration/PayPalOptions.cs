namespace Online_Mobile_Recharge.Models.Configuration;

public class PayPalOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Environment { get; set; } = "Sandbox"; // Sandbox / Live
}

