# Online Mobile Recharge

ASP.NET Core MVC application for prepaid top-ups and postpaid bill payments, with user/admin dashboards, notifications, and email flows (verification, password reset, reminders).

## Features

- **User**
  - Browse products and purchase prepaid top-ups / packages
  - Create and pay **postpaid** bills with due dates + reminders
  - Dashboard with spending chart, transactions, and alerts (due soon / overdue)
  - Account management (profile, settings, password change)
- **Admin**
  - User management (lock/unlock users, view lock reason)
  - Audit logs (admin actions)
  - Reports endpoints (trend / revenue / best sellers)
  - SMTP diagnostics endpoints (see **SMTP troubleshooting**)
- **Email**
  - Email verification
  - Password reset
  - Transaction confirmation
  - Postpaid reminder
  - Support inbox notification

## Tech stack

- **.NET / ASP.NET Core MVC** (`net8.0`)
- **Entity Framework Core** (SQL Server)
- **MailKit** (SMTP)
- **Bootstrap 5** UI

## Requirements

- .NET SDK 8+
- SQL Server or LocalDB

## Quick start

1. Restore & run:

   ```powershell
   dotnet restore
   dotnet run
   ```

2. Open the site:
   - `https://localhost:7135` (default dev)

On startup the app applies migrations and seeds sample data (see `Program.cs`).

## Configuration

Configuration is loaded from `appsettings.json` and can be overridden via environment variables.

### Database

`appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MobileRechargeDB;Trusted_Connection=True;"
}
```

### SMTP (Email)

`appsettings.json` contains an `Email:Smtp` section. For real deployments, do **not** commit secrets—prefer **environment variables** or **User Secrets**.

Environment variable naming (double underscore for nesting):

```powershell
$env:Email__Smtp__Host = "smtp.gmail.com"
$env:Email__Smtp__Port = "587"
$env:Email__Smtp__EnableSsl = "true"
$env:Email__Smtp__UserName = "your-account@gmail.com"
$env:Email__Smtp__Password = "YOUR_16_CHAR_APP_PASSWORD"
$env:Email__Smtp__FromEmail = "your-account@gmail.com"
$env:Email__Smtp__FromName = "Online Mobile Recharge"
```

If you use Gmail:

- Turn on **2‑Step Verification** for the Google Account.
- Generate an **App Password** and use that value as `Email__Smtp__Password`.
- Do not use your normal Gmail password.

### PayPal

`appsettings.json` includes a `PayPal` section (`ClientId`, `Secret`, `Environment`). Use environment variables for production secrets.

## SMTP troubleshooting

The app includes admin diagnostics endpoints to pinpoint failures:

- `GET /Admin/SmtpReport`
  - Shows whether SMTP is configured and which host/port/username/from address are loaded (does **not** return the password).
- `GET /Admin/SmtpProbe`
  - Attempts connect + authenticate and returns:
    - `stage="config"` → missing config
    - `stage="connect"` → DNS/firewall/TLS/port issues
    - `stage="auth"` → username/password rejected
- `POST /Admin/SmtpTest`
  - Sends a test email and returns `{ sent, error }`.

Common Gmail error:

- `535 5.7.8 Username and Password not accepted`  
  Use an **App Password** (requires 2FA) and ensure `UserName` matches the account that generated it.

## Security notes

- Password reset tokens / verification links must be treated as secrets. If a link is posted publicly, generate a new token immediately.
- Do not store SMTP passwords or PayPal secrets in source control.

## Development notes

- The project defines a `UserSecretsId` (see `Online_Mobile_Recharge.csproj`). You can use:

  ```powershell
  dotnet user-secrets set "Email:Smtp:Password" "YOUR_APP_PASSWORD"
  ```

- If you have a running instance, `dotnet build` may fail due to a locked `Online_Mobile_Recharge.exe`. Stop the running process and rebuild.

## Project structure (high level)

- `Controllers/` MVC controllers (Account, User, Admin, Checkout, etc.)
- `Models/` Entities + view models + configuration options
- `Services/` Business logic, notifications, email, payments
- `Views/` Razor views
- `Data/` DB initializer + seeding

## License

Internal / educational project (add your license terms here if needed).

