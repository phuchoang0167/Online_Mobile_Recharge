using Online_Mobile_Recharge.Models.Enums;

namespace Online_Mobile_Recharge.Models.Entities
{
    public class Sale
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ProductSaleType SaleType { get; set; } = ProductSaleType.None;
        public decimal SaleValue { get; set; }

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}

