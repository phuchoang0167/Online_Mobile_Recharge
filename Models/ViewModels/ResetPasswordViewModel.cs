using System.ComponentModel.DataAnnotations;

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a new password.")]
    [MinLength(6, ErrorMessage = "The new password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the new password.")]
    [Compare("NewPassword", ErrorMessage = "Password confirmation does not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
