using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class UserAuthorizeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var httpContext = context.HttpContext;

        var claimUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var claimRole = httpContext.User.FindFirstValue(ClaimTypes.Role);

        var sessionUserId = httpContext.Session.GetInt32("UserId");
        var sessionRole = httpContext.Session.GetString("Role");

        var role = claimRole ?? sessionRole;
        var userId = int.TryParse(claimUserId, out var parsedUserId) ? parsedUserId : sessionUserId;

        if (userId == null)
        {
            var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("Dashboard", "Admin", null);
            return;
        }

        if (!string.Equals(role, "User", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var dbContext = httpContext.RequestServices.GetRequiredService<MobileRechargeDbContext>();
        var user = dbContext.Users
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == userId.Value && !x.IsDeleted && x.IsActive);

        if (user == null || !string.Equals(user.Role, "User", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Session.Clear();
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
