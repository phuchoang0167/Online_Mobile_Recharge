using System.ComponentModel.DataAnnotations;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
