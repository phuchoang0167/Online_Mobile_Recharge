using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class CallerTunePageViewModel
{
    public DateTime Now { get; init; } = DateTime.Now;

    public bool HasActiveSubscription { get; init; }
    public DateTime? SubscriptionExpiresAt { get; init; }

    public int? SubscriptionProductId { get; init; }

    public int? SelectedCallerTuneId { get; init; }
    public bool CallerTuneEnabled { get; init; }

    public List<CallerTune> Tunes { get; init; } = new();
}

