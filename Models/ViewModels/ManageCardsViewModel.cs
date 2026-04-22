using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.ViewModels;

public class ManageCardsViewModel
{
    public List<Card> Cards { get; set; } = new();

    public CardFormViewModel Form { get; set; } = new();

    public bool IsEditMode => Form.Id.HasValue;
}
