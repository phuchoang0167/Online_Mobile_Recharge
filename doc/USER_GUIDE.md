# User Guide

## Purpose
- Explain how to use the main features of the website.
- Focus on the normal flow for visitors, users, and admins.

## For Visitors
- Open the home page at `/`
- Browse services from `/Product`
- Read support content from:
  - `/Home/HowItWorks`
  - `/Home/Faq`
  - `/Home/Contact`
  - `/Home/Sitemap`

## Create an Account
- Open `/Account/Register`
- Enter name, email, password, and phone number
- Verify the email from your inbox
- If email delivery fails in local demo mode, use the fallback verification link shown on the login page

## Sign In
- Open `/Account/Login`
- Enter your email and password
- If you forget the password, open `/Account/ResetPassword`
- Use the reset link from email or the demo fallback link on the login page

## Buy a Product
- Open `/Product`
- Select a phone number first (top right `Select phone` or `/Phone/Select`)
- Choose a product and open the detail page
- Click the buy button to go to checkout
- Choose one payment flow:
  - `Prepaid` for immediate card payment
  - `Postpaid` to create a pending bill for later payment
- Confirm the action in the on-screen modal

### Guest (Not Logged In)
- Guests can only purchase `Topup` (recharge) products from `/Product`.
- Guest topup checkout uses `/GuestCheckout` (manual demo card entry).

### Postpaid Required Info
When choosing `Postpaid`, you must provide:
- `National ID / CCCD` (9 or 12 digits)
- `Billing address`
- Agree to the postpaid terms
These values are stored on the postpaid `Transaction` and also saved to your user profile for future prefilling.

## Pay a Postpaid Bill
- Open `/User/Transaction`
- Find a transaction with `Pending` status
- Click `Pay Now`
- Choose a saved card (or enter card number, expiry, and CVV manually)
- Confirm payment

## Demo Cards
- Open `/User/Cards`
- Add one of the sample cards from `doc/DEMO_DATA.md`
- You may create, edit, or delete saved cards anytime
- You may edit card number, CVV, and expiry date
- Demo balance is set automatically from `doc/DEMO_DATA.md`
- During checkout, you can select a saved card directly (or enter the same saved card details manually)
- Balance is deducted automatically by prepaid checkout and postpaid bill payment
- Use `doc/DEMO_DATA.md` for the demo accounts and 5 sample cards to add

## Export a Bill
- After checkout, use `Print / Save Bill as PDF`
- Or open `/User/Transaction` and click `Bill PDF`
- In the browser print dialog, choose `Save as PDF`

## Leave Feedback
- Buy a product successfully first
- Return to the product detail page
- Submit feedback on the purchased product
- View your feedback again from:
  - the product detail page
  - `/Feedback`
- You can edit your feedback within 1 day

## Use the User Dashboard
- Open `/User/Dashboard`
- Review:
  - transactions summary (clickable cards)
  - pending postpaid bills / due soon reminders
  - saved cards balance
  - feedback shortcuts
- Open related pages from the dashboard or sidebar:
  - `/User/Profile`
  - `/User/Transaction`
  - `/User/Cards`
  - `/User/Dnd`
  - `/User/CallerTune`
  - `/Feedback`

## Use the Admin Dashboard
- Open `/Admin/Dashboard`
- Review:
  - users
  - revenue
  - overdue bills
  - recent transactions
  - recent feedback
- Manage the system from:
  - `/Admin/Users`
  - `/Admin/Transaction`
  - `/Admin/Feedbacks`

## Notifications
- Open the bell icon in the top area
- Notifications summarize:
  - pending or overdue postpaid bills
  - product feedback activity
  - DND and caller tune reminders
- Once opened, the unread badge should disappear until new items appear

## Notes
- Email sending depends on SMTP configuration.
- In local demo mode, verify/reset flows can still use fallback links if email fails.
- The project already includes seeded products and demo data for testing.
