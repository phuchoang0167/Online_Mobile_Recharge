namespace Online_Mobile_Recharge.Models.Entities
{
    public class DndNumber
    {
        public int Id { get; set; }

        public int DndSettingId { get; set; }
        public DndSetting DndSetting { get; set; } = null!;

        public string PhoneNumber { get; set; } = string.Empty;
    }
}
