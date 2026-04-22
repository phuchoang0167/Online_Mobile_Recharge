namespace Online_Mobile_Recharge.Models.ViewModels;

public class UiBannerViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string IconClass { get; set; } = "bi bi-stars";
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
}

