namespace Online_Mobile_Recharge.Models.ViewModels
{
    public class ProductPricingInfoViewModel
    {
        public int ProductId { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal EffectivePrice { get; set; }
        public bool HasSale { get; set; }
        public string? SaleBadgeText { get; set; }
    }
}

