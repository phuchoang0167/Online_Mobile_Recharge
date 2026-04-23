using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Services;

[Route("Admin/CallerTune")]
[AdminAuthorize]
public class AdminCallerTuneController : Controller
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private readonly MobileRechargeDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly AdminAuditService _audit;

    public AdminCallerTuneController(MobileRechargeDbContext context, IWebHostEnvironment env, AdminAuditService audit)
    {
        _context = context;
        _env = env;
        _audit = audit;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var tunes = _context.CallerTunes
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return View(tunes);
    }

    [HttpPost("Upload")]
    [ValidateAntiForgeryToken]
    public IActionResult Upload(string title, IFormFile file)
    {
        var adminId = HttpContext.Session.GetInt32("UserId")!.Value;

        var normalizedTitle = (title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            TempData["Error"] = "Please enter a title for this caller tune.";
            return RedirectToAction(nameof(Index));
        }

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please choose an MP3 file before uploading.";
            return RedirectToAction(nameof(Index));
        }

        if (!file.FileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only MP3 files are supported.";
            return RedirectToAction(nameof(Index));
        }

        if (file.Length > MaxFileSizeBytes)
        {
            TempData["Error"] = "MP3 files must be 5MB or smaller.";
            return RedirectToAction(nameof(Index));
        }

        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var uploadPath = Path.Combine(_env.WebRootPath, "uploads");

        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        var path = Path.Combine(uploadPath, fileName);

        using (var stream = new FileStream(path, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        var tune = new CallerTune
        {
            CreatedByAdminId = adminId,
            Title = normalizedTitle,
            FilePath = "/uploads/" + fileName,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.CallerTunes.Add(tune);
        _context.SaveChanges();

        _audit.LogAsync(
            adminUserId: adminId,
            action: "Create",
            summary: $"Uploaded caller tune #{tune.Id}.",
            oldData: (CallerTune?)null,
            newData: new CallerTune
            {
                Id = tune.Id,
                CreatedByAdminId = tune.CreatedByAdminId,
                Title = tune.Title,
                FilePath = tune.FilePath,
                IsActive = tune.IsActive,
                CreatedAt = tune.CreatedAt
            },
            entityId: tune.Id).GetAwaiter().GetResult();

        TempData["Success"] = "New caller tune added to catalog.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle")]
    [ValidateAntiForgeryToken]
    public IActionResult Toggle(int id)
    {
        var tune = _context.CallerTunes.FirstOrDefault(x => x.Id == id);
        if (tune == null)
        {
            return NotFound();
        }

        var oldData = new CallerTune
        {
            Id = tune.Id,
            CreatedByAdminId = tune.CreatedByAdminId,
            Title = tune.Title,
            FilePath = tune.FilePath,
            IsActive = tune.IsActive,
            CreatedAt = tune.CreatedAt
        };

        tune.IsActive = !tune.IsActive;
        _context.SaveChanges();

        _audit.LogAsync(
            adminUserId: HttpContext.Session.GetInt32("UserId") ?? 0,
            action: "Update",
            summary: $"{(tune.IsActive ? "Enabled" : "Disabled")} caller tune #{tune.Id}.",
            oldData: oldData,
            newData: new CallerTune
            {
                Id = tune.Id,
                CreatedByAdminId = tune.CreatedByAdminId,
                Title = tune.Title,
                FilePath = tune.FilePath,
                IsActive = tune.IsActive,
                CreatedAt = tune.CreatedAt
            },
            entityId: tune.Id).GetAwaiter().GetResult();

        TempData["Success"] = tune.IsActive
            ? "Caller tune is now available for users."
            : "Caller tune has been hidden from users.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var tune = _context.CallerTunes.FirstOrDefault(x => x.Id == id);
        if (tune == null)
        {
            return NotFound();
        }

        var oldData = new CallerTune
        {
            Id = tune.Id,
            CreatedByAdminId = tune.CreatedByAdminId,
            Title = tune.Title,
            FilePath = tune.FilePath,
            IsActive = tune.IsActive,
            CreatedAt = tune.CreatedAt
        };

        var relativePath = (tune.FilePath ?? string.Empty).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.GetFullPath(Path.Combine(_env.WebRootPath, relativePath));
        var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));

        if (absolutePath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }

        _context.CallerTunes.Remove(tune);
        _context.SaveChanges();

        _audit.LogAsync(
            adminUserId: HttpContext.Session.GetInt32("UserId") ?? 0,
            action: "Delete",
            summary: $"Deleted caller tune #{oldData.Id}.",
            oldData: oldData,
            newData: (CallerTune?)null,
            entityId: oldData.Id).GetAwaiter().GetResult();

        TempData["Success"] = "Caller tune removed from catalog.";
        return RedirectToAction(nameof(Index));
    }
}
