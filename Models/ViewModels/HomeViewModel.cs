using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class HomeViewModel
{
    public int TotalUsers { get; set; }

    public int TotalTransactions { get; set; }

    public decimal SuccessRate { get; set; }

    public List<Product> FeaturedProducts { get; set; } = new();

    public List<FaqItemViewModel> FaqItems { get; set; } = new();
}
