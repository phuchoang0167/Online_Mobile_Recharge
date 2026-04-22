using Online_Mobile_Recharge.Models.Enums;

namespace Online_Mobile_Recharge.Models.Entities
{
    public class Transaction
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string PhoneNumber { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public TransactionType Type { get; set; } // Prepaid / Postpaid

        public PaymentMethod PaymentMethod { get; set; }
        public TransactionStatus Status { get; set; }

        public DateTime? DueDate { get; set; }  
        public bool IsPaid { get; set; }

        public string? PostpaidNationalId { get; set; }
        public string? PostpaidBillingAddress { get; set; }
        public DateTime? PostpaidAgreedAt { get; set; }
        public DateTime? PostpaidContractAgreedAt { get; set; }

        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
