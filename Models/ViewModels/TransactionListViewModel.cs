using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class TransactionListViewModel
{
    public string Keyword { get; set; } = string.Empty;
    public string Status { get; set; } = "all";
    public string Type { get; set; } = "all";
    public string Range { get; set; } = "all";
    public List<Transaction> Items { get; set; } = new();

    public List<SavedCardOptionViewModel> SavedCards { get; set; } = new();
    public int? DefaultSavedCardId { get; set; }
}
