using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

public class AdminAuthorizeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserId");
        var role = context.HttpContext.Session.GetString("Role");

        if (userId == null || role != "Admin")
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<MobileRechargeDbContext>();
        var admin = dbContext.Users
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == userId.Value && !x.IsDeleted && x.IsActive);

        if (admin == null || admin.Role != "Admin")
        {
            context.HttpContext.Session.Clear();
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
