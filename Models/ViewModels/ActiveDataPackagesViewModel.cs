namespace Online_Mobile_Recharge.Models.ViewModels;

public class ActiveDataPackagesViewModel
{
    public DateTime Now { get; set; } = DateTime.Now;
    public List<ActiveDataPackageGroupViewModel> Groups { get; set; } = new();
}

public class ActiveDataPackageGroupViewModel
{
    public string PhoneNumber { get; set; } = string.Empty;
    public List<ActiveDataPackageItemViewModel> Items { get; set; } = new();
}

public class ActiveDataPackageItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime ActivatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

