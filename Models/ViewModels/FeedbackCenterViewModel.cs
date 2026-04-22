using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class FeedbackCenterViewModel
{
    public bool IsLoggedIn { get; set; }

    public int PurchasedProductsCount { get; set; }

    public List<Feedback> MyFeedbacks { get; set; } = new();
}
