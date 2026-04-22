namespace Online_Mobile_Recharge.Models.Entities
{
    public class Feedback
    {
        public int Id { get; set; }

        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? AdminReply { get; set; }
        public DateTime? AdminRepliedAt { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
        public int? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
