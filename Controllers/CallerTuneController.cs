using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Models.ViewModels;

[Route("User/CallerTune")]
[UserAuthorize]
public class CallerTuneController : Controller
{
    private readonly MobileRechargeDbContext _context;

    public CallerTuneController(MobileRechargeDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var now = DateTime.Now;

        var subscription = _context.CallerTuneSubscriptions
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId);

        var hasActiveSubscription = subscription != null && subscription.ExpiresAt > now;

        var setting = _context.UserCallerTuneSettings
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId);

        var subscriptionProductId = _context.Products
            .AsNoTracking()
            .Where(x => x.Type == ProductType.CallerTune)
            .OrderBy(x => x.Price)
            .Select(x => (int?)x.Id)
            .FirstOrDefault();

        var tunes = _context.CallerTunes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return View(new CallerTunePageViewModel
        {
            Now = now,
            HasActiveSubscription = hasActiveSubscription,
            SubscriptionExpiresAt = subscription?.ExpiresAt,
            SubscriptionProductId = subscriptionProductId,
            SelectedCallerTuneId = setting?.CallerTuneId,
            CallerTuneEnabled = setting?.IsEnabled ?? false,
            Tunes = tunes
        });
    }

    [HttpPost("Select")]
    [ValidateAntiForgeryToken]
    public IActionResult Select(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var now = DateTime.Now;

        var hasActiveSubscription = _context.CallerTuneSubscriptions
            .AsNoTracking()
            .Any(x => x.UserId == userId && x.ExpiresAt > now);

        if (!hasActiveSubscription)
        {
            TempData["Error"] = "Please subscribe to Caller Tune monthly plan before selecting a tune.";
            return RedirectToAction(nameof(Index));
        }

        var tuneExists = _context.CallerTunes.Any(x => x.Id == id && x.IsActive);
        if (!tuneExists)
        {
            return NotFound();
        }

        var setting = _context.UserCallerTuneSettings.FirstOrDefault(x => x.UserId == userId);
        if (setting == null)
        {
            setting = new Online_Mobile_Recharge.Models.Entities.UserCallerTuneSetting
            {
                UserId = userId,
                CallerTuneId = id,
                IsEnabled = true,
                UpdatedAt = now
            };
            _context.UserCallerTuneSettings.Add(setting);
        }
        else
        {
            setting.CallerTuneId = id;
            setting.IsEnabled = true;
            setting.UpdatedAt = now;
        }

        _context.SaveChanges();

        TempData["Success"] = "Caller tune selected successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Disable")]
    [ValidateAntiForgeryToken]
    public IActionResult Disable()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var now = DateTime.Now;

        var setting = _context.UserCallerTuneSettings.FirstOrDefault(x => x.UserId == userId);
        if (setting == null)
        {
            setting = new Online_Mobile_Recharge.Models.Entities.UserCallerTuneSetting
            {
                UserId = userId,
                CallerTuneId = null,
                IsEnabled = false,
                UpdatedAt = now
            };
            _context.UserCallerTuneSettings.Add(setting);
        }
        else
        {
            setting.IsEnabled = false;
            setting.UpdatedAt = now;
        }

        _context.SaveChanges();

        TempData["Success"] = "Caller tune has been turned off.";
        return RedirectToAction(nameof(Index));
    }
}

