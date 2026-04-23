using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Online_Mobile_Recharge.Models.Configuration;
using Online_Mobile_Recharge.Services;
using Online_Mobile_Recharge.Services.Auditing;

var builder = WebApplication.CreateBuilder(args);
var smtpSection = builder.Configuration.GetSection("Email:Smtp");
var appUrlSection = builder.Configuration.GetSection("App");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EntityAuditSaveChangesInterceptor>();
builder.Services.AddDbContext<MobileRechargeDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddInterceptors(sp.GetRequiredService<EntityAuditSaveChangesInterceptor>()));
builder.Services.Configure<SmtpOptions>(smtpSection);
builder.Services.Configure<AppUrlOptions>(appUrlSection);
builder.Services.Configure<PayPalOptions>(builder.Configuration.GetSection("PayPal"));

builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<DndService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<EmailNotificationService>();
builder.Services.AddHttpClient<PayPalService>();
builder.Services.AddScoped<AdminAuditService>();
builder.Services.AddSingleton<IValidateOptions<PayPalOptions>, PayPalOptionsValidation>();

builder.Services.AddSession();

var authenticationBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MobileRechargeDbContext>();
    context.Database.Migrate();
    DbInitializer.Seed(context);
}
app.UseHttpsRedirection();
app.UseHsts();

app.UseSession();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
