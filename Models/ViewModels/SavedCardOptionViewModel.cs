namespace Online_Mobile_Recharge.Models.ViewModels;

public class SavedCardOptionViewModel
{
    public int Id { get; set; }
    public string MaskedNumber { get; set; } = string.Empty;
    public string Expiry { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Label { get; set; } = string.Empty;
}

