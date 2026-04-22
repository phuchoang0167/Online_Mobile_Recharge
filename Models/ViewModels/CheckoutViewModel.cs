using System.ComponentModel.DataAnnotations;
using Online_Mobile_Recharge.Models.ViewModels;

public class CheckoutViewModel
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? UserName { get; set; }
    public string? DefaultPhone { get; set; }
    public List<string> PhoneOptions { get; set; } = new();
    public string? SelectedPhoneOption { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal Price { get; set; }
    public bool HasSale { get; set; }
    public string? SaleBadgeText { get; set; }

    [RegularExpression(@"^\d{9}(\d{3})?$", ErrorMessage = "National ID / CCCD must be 9 or 12 digits.")]
    public string? PostpaidNationalId { get; set; }

    public string? PostpaidBillingAddress { get; set; }

    public bool PostpaidAgreeToTerms { get; set; }

    public bool PostpaidAgreeToContract { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    public string? CardNumber { get; set; }

    public string? CVV { get; set; }

    public string? Expiry { get; set; }

    public string CardInputMode { get; set; } = "saved";

    public int? SavedCardId { get; set; }

    public List<SavedCardOptionViewModel> SavedCards { get; set; } = new();

    public string PrepaidPaymentMethod { get; set; } = "card";
}
