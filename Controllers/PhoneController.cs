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

        if (!Regex.IsMatch(model.Phone, @"^0\d{9}$"))
        {
            ModelState.AddModelError(nameof(model.Phone), "Số điện thoại không hợp lệ (định dạng 0xxxxxxxxx).");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        HttpContext.Session.SetString(SelectedPhoneSessionKey, model.Phone);
        TempData["SuccessMessage"] = "Đã chọn số điện thoại.";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Product");
    }
}
