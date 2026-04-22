namespace Online_Mobile_Recharge.Models.ViewModels;

public class UserProfileViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? BillingAddress { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}
