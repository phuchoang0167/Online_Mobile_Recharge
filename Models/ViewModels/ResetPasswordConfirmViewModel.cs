using System.ComponentModel.DataAnnotations;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class ResetPasswordConfirmViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a new password.")]
    [MinLength(6, ErrorMessage = "The new password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the new password.")]
    [Compare("NewPassword", ErrorMessage = "The password confirmation does not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
