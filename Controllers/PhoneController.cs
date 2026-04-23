using Microsoft.AspNetCore.Mvc;
using Online_Mobile_Recharge.Models.ViewModels;
using System.Text.RegularExpressions;

namespace Online_Mobile_Recharge.Controllers;

public class PhoneController : Controller
{
    private const string SelectedPhoneSessionKey = "SelectedPhoneNumber";

    [HttpGet]
    public IActionResult Select(string? returnUrl = null)
    {
        var existing = HttpContext.Session.GetString(SelectedPhoneSessionKey) ?? string.Empty;
        return View(new PhoneSelectionViewModel
        {
            Phone = existing,
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Index", "Product") : returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Select(PhoneSelectionViewModel model)
    {
        model.Phone = (model.Phone ?? string.Empty).Trim();
        model.ReturnUrl = string.IsNullOrWhiteSpace(model.ReturnUrl) ? Url.Action("Index", "Product") : model.ReturnUrl;

        var normalizedPhone = Regex.Replace(model.Phone, @"[^\d]", string.Empty);
        if (!Regex.IsMatch(normalizedPhone, @"^0\d{9}$"))
        {
            ModelState.AddModelError(nameof(model.Phone), "Invalid phone number (format: 0xxxxxxxxx).");
        }

        if (!ModelState.IsValid)
        {
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                TempData["ErrorMessage"] = "Please enter a valid phone number (format: 0xxxxxxxxx).";
                return LocalRedirect(model.ReturnUrl);
            }

            return View(model);
        }

        HttpContext.Session.SetString(SelectedPhoneSessionKey, normalizedPhone);
        TempData["SuccessMessage"] = "Phone number selected.";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Product");
    }
}
