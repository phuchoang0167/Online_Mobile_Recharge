using System.ComponentModel.DataAnnotations;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class ProductFeedbackFormViewModel
{
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Please enter your feedback.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Feedback must be between 10 and 1000 characters.")]
    public string Message { get; set; } = string.Empty;
}
