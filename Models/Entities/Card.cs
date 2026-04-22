using System.ComponentModel.DataAnnotations.Schema;

namespace Online_Mobile_Recharge.Models.Entities
{
    public class Card
    {
        public int Id { get; set; }

        public string CardNumber { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;

        public decimal Balance { get; set; }

        public decimal Price { get; set; }

        public DateTime ExpiryDate { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
