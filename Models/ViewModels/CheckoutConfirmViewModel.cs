using System.ComponentModel.DataAnnotations;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class CheckoutConfirmViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    public bool PostpaidAgreeToTerms { get; set; }

    public bool PostpaidAgreeToContract { get; set; }
}

