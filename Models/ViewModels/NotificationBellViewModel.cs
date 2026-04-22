namespace Online_Mobile_Recharge.Models.ViewModels;

public class NotificationBellViewModel
{
    public string Heading { get; set; } = "Notifications";

    public int Count { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public string CookieKey { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public List<NotificationItemViewModel> Items { get; set; } = new();
}

public class NotificationItemViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Icon { get; set; } = "bi-info-circle";

    public string Tone { get; set; } = "primary";
}
