using Microsoft.AspNetCore.Mvc;
using Online_Mobile_Recharge.Models.Enums;
using System.Text.RegularExpressions;

[Route("User/Dnd")]
[UserAuthorize]
public class DndController : Controller
{
    private readonly DndService _service;

    public DndController(DndService service)
    {
        _service = service;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = _service.GetOrCreate(userId.Value);
        return View(model);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public IActionResult Save(bool isEnabled, DndMode mode, string numbers)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var list = string.IsNullOrEmpty(numbers)
            ? new List<string>()
            : numbers
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

        var invalidNumbers = list
            .Where(x => !Regex.IsMatch(x, @"^0\d{9}$"))
            .ToList();

        if (mode == DndMode.Custom && invalidNumbers.Any())
        {
            TempData["Error"] = $"Invalid phone number(s): {string.Join(", ", invalidNumbers)}";
            return RedirectToAction(nameof(Index));
        }

        _service.Update(userId, isEnabled, mode, list);
        TempData["Success"] = "DND settings updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Add")]
    [ValidateAntiForgeryToken]
    public IActionResult Add(string phone)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var normalizedPhone = (phone ?? string.Empty).Trim();

        if (!Regex.IsMatch(normalizedPhone, @"^0\d{9}$"))
        {
            TempData["Error"] = "Phone number must be 10 digits and start with 0.";
            return RedirectToAction(nameof(Index));
        }

        _service.AddNumber(userId, normalizedPhone);
        TempData["Success"] = "Phone number added to the DND list.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        _service.DeleteNumber(userId, id);
        TempData["Success"] = "Phone number removed from the DND list.";

        return RedirectToAction(nameof(Index));
    }
}
