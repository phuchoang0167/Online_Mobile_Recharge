# SMTP Setup

## Recommended Option
- Use `Gmail SMTP` for demo.
- Use a Gmail account with `2-Step Verification` enabled.
- Create an `App Password` and use that password in the config.

## appsettings.json
```json
"Email": {
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UserName": "yourgmail@gmail.com",
    "Password": "your-16-char-app-password",
    "FromEmail": "yourgmail@gmail.com",
    "FromName": "Online Mobile Recharge",
    "SupportEmail": "yourgmail@gmail.com",
    "EnableSsl": true
  }
}
```

## Gmail Notes
- `Host` must be `smtp.gmail.com`
- `Port` should be `587`
- `UserName` should be the Gmail address
- `Password` should be the Gmail `App Password`
- `EnableSsl` should be `true`
- The app now uses `STARTTLS` on port `587`
- In SMTP test tools, use `STARTTLS` or `TLS when available`

## Quick Test
- Restart the app after changing config
- Test `/Account/Register`
- Test `/Account/ResetPassword`
- Test `/Home/Contact`
- Check both `Inbox` and `Spam`

## If Email Still Fails
- Recheck `2-Step Verification`
- Recreate the Gmail `App Password`
- Make sure `UserName` and `FromEmail` match the same Gmail account
- For demo, the project still supports fallback verification/reset links on the login page
