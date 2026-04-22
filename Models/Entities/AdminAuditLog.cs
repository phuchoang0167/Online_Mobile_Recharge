namespace Online_Mobile_Recharge.Models.Entities;

public class AdminAuditLog
{
    public int Id { get; set; }

    public int AdminUserId { get; set; }

    public string EntityType { get; set; } = string.Empty; // Product / ProductSale / Sale
    public int EntityId { get; set; }

    public string Action { get; set; } = string.Empty; // Create / Update

    public string Summary { get; set; } = string.Empty;

    public string? OldDataJson { get; set; }
    public string? NewDataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

