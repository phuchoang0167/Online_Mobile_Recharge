using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class AdminTransactionHistoryViewModel
{
    public string Keyword { get; set; } = string.Empty;
    public string Status { get; set; } = "all";
    public string Range { get; set; } = "all";

    public int? SelectedUserId { get; set; }
    public User? SelectedUser { get; set; }

    public List<User> Users { get; set; } = new();
    public List<Transaction> PrepaidTransactions { get; set; } = new();
    public List<Transaction> PostpaidTransactions { get; set; } = new();
}
