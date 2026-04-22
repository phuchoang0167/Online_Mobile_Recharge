using System.ComponentModel.DataAnnotations;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class ContactViewModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your message.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Message should be between 10 and 1000 characters.")]
    public string Message { get; set; } = string.Empty;
}
