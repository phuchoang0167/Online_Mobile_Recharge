using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;

public class DndService
{
    private readonly MobileRechargeDbContext _context;

    public DndService(MobileRechargeDbContext context)
    {
        _context = context;
    }

    public DndSetting GetOrCreate(int userId)
    {
        var setting = _context.DndSettings
            .Include(x => x.Numbers)
            .FirstOrDefault(x => x.UserId == userId);

        if (setting == null)
        {
            setting = new DndSetting
            {
                UserId = userId,
                IsEnabled = false,
                Mode = DndMode.AllowAll,
                Numbers = new List<DndNumber>()
            };

            _context.DndSettings.Add(setting);
            _context.SaveChanges();
        }

        return setting;
    }

    public void Update(int userId, bool isEnabled, DndMode mode, List<string> numbers)
    {
        var setting = GetOrCreate(userId);

        setting.IsEnabled = isEnabled;
        setting.Mode = mode;

        _context.DndNumbers.RemoveRange(setting.Numbers);

        if (mode == DndMode.Custom && numbers != null)
        {
            setting.Numbers = numbers.Select(n => new DndNumber
            {
                PhoneNumber = n,
                DndSettingId = setting.Id
            }).ToList();
        }

        _context.SaveChanges();
    }

    public void AddNumber(int userId, string phone)
    {
        var setting = GetOrCreate(userId);

        var exists = _context.DndNumbers
            .Any(x => x.DndSettingId == setting.Id && x.PhoneNumber == phone);

        if (!exists)
        {
            _context.DndNumbers.Add(new DndNumber
            {
                PhoneNumber = phone,
                DndSettingId = setting.Id
            });

            _context.SaveChanges();
        }
    }

    public void DeleteNumber(int userId, int numberId)
    {
        var setting = GetOrCreate(userId);

        var number = _context.DndNumbers
            .FirstOrDefault(x => x.Id == numberId && x.DndSettingId == setting.Id);

        if (number != null)
        {
            _context.DndNumbers.Remove(number);
            _context.SaveChanges();
        }
    }
}