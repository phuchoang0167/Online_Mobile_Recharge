using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your phone number.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "The phone number must contain 10 digits and start with 0.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a password.")]
    [MinLength(6, ErrorMessage = "The password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "The password confirmation does not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
