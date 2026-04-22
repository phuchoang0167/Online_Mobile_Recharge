using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class TransactionInvoiceViewModel
{
    public Transaction Transaction { get; set; } = null!;
    public User User { get; set; } = null!;
    public string ProductName { get; set; } = "Service transaction";
}
