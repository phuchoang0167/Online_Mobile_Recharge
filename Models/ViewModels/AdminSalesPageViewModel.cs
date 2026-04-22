using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.ViewModels
{
    public class AdminSalesPageViewModel
    {
        public List<Sale> Sales { get; set; } = new();
        public List<ProductSale> ProductSales { get; set; } = new();

        public string Filter { get; set; } = "active";
        public int? ProductId { get; set; }
    }
}

