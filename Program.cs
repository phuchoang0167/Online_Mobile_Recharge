using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Configuration;
using Online_Mobile_Recharge.Services;

var builder = WebApplication.CreateBuilder(args);
var smtpSection = builder.Configuration.GetSection("Email:Smtp");
var appUrlSection = builder.Configuration.GetSection("App");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MobileRechargeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<SmtpOptions>(smtpSection);
builder.Services.Configure<AppUrlOptions>(appUrlSection);
builder.Services.Configure<PayPalOptions>(builder.Configuration.GetSection("PayPal"));

builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<DndService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<EmailNotificationService>();
builder.Services.AddScoped<PayPalService>();
builder.Services.AddScoped<AdminAuditService>();

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

app.UseSession();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
