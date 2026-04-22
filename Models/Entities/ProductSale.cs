using Online_Mobile_Recharge.Models.Enums;

namespace Online_Mobile_Recharge.Models.Entities
{
    public class ProductSale
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public ProductSaleType SaleType { get; set; } = ProductSaleType.None;
        public decimal SaleValue { get; set; }

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}

