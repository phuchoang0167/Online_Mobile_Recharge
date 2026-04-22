namespace Online_Mobile_Recharge.Models.Entities;

public class UserCallerTuneSetting
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? CallerTuneId { get; set; }
    public CallerTune? CallerTune { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

