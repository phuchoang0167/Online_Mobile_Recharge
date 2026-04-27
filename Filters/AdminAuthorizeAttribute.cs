using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class AdminAuthorizeAttribute : ActionFilterAttribute
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

        if (userId == null || !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var dbContext = httpContext.RequestServices.GetRequiredService<MobileRechargeDbContext>();
        var admin = dbContext.Users
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == userId.Value && !x.IsDeleted && x.IsActive);

        if (admin == null || !string.Equals(admin.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Session.Clear();
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
