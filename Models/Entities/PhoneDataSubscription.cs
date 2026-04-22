namespace Online_Mobile_Recharge.Models.Entities;

public class PhoneDataSubscription
{
    public int Id { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateTime ActivatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

