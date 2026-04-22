using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Online_Mobile_Recharge.Models.Entities;

public class CallerTune
{
    public int Id { get; set; }

    // Backward-compatible column name (was UserId when this table stored per-user uploads).
    [Column("UserId")]
    public int CreatedByAdminId { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    // Used as "available in catalog".
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}

