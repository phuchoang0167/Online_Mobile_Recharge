namespace Online_Mobile_Recharge.Models.ViewModels;

public class CheckoutReviewViewModel
{
    public string Token { get; set; } = string.Empty;

    public CheckoutViewModel Checkout { get; set; } = new();

    public string? MaskedCardNumber { get; set; }
    public string? CardLabel { get; set; }
}

