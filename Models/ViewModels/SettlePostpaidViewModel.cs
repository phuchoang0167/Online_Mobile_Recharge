using System.ComponentModel.DataAnnotations;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class SettlePostpaidViewModel
{
    public int TransactionId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime? DueDate { get; set; }

    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the card number.")]
    [RegularExpression(@"^\d{16}$", ErrorMessage = "The card number must contain exactly 16 digits.")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the expiry date.")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "The expiry date must use the MM/YY format.")]
    public string Expiry { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the CVV.")]
    [RegularExpression(@"^\d{3}$", ErrorMessage = "The CVV is invalid.")]
    public string CVV { get; set; } = string.Empty;

    public string CardInputMode { get; set; } = "saved";

    public int? SavedCardId { get; set; }

    public List<SavedCardOptionViewModel> SavedCards { get; set; } = new();
}
