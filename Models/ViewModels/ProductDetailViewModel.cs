using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class ProductDetailViewModel
{
    public Product Product { get; set; } = null!;

    public List<Feedback> Feedbacks { get; set; } = new();

    public ProductFeedbackFormViewModel FeedbackForm { get; set; } = new();

    public Feedback? MyFeedback { get; set; }

    public bool IsLoggedIn { get; set; }

    public bool HasPurchased { get; set; }

    public bool CanSubmitFeedback { get; set; }

    public bool CanEditFeedback { get; set; }

    public DateTime? EditDeadline { get; set; }
}
