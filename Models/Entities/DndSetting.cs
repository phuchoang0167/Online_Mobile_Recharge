using Online_Mobile_Recharge.Models.Enums;

namespace Online_Mobile_Recharge.Models.Entities
{
    public class DndSetting
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public bool IsEnabled { get; set; }
        public DndMode Mode { get; set; }

        public ICollection<DndNumber> Numbers { get; set; } = new List<DndNumber>();
    }
}
