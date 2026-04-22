using Online_Mobile_Recharge.Models.Enums;

namespace Online_Mobile_Recharge.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public ProductType Type { get; set; } // DATA / TOPUP

        public int? ValidDays { get; set; }

        public bool IsSpecial { get; set; }
        public bool IsTop { get; set; }

        public ICollection<ProductSale> Sales { get; set; } = new List<ProductSale>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
