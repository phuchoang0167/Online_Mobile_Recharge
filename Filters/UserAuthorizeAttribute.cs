using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

public class UserAuthorizeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserId");
        var role = context.HttpContext.Session.GetString("Role");

        if (userId == null)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (role == "Admin")
        {
            context.Result = new RedirectToActionResult("Dashboard", "Admin", null);
            return;
        }

        if (role != "User")
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<MobileRechargeDbContext>();
        var user = dbContext.Users
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == userId.Value && !x.IsDeleted && x.IsActive);

        if (user == null || user.Role != "User")
        {
            context.HttpContext.Session.Clear();
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
