# Test Guide

## Purpose
- Quick guide to demo and test the main flows of the project.
- Focus on the screens and actions that matter most in class/demo sessions.

## Test Accounts
- `Admin`
  - use the seeded admin account in the local database
- `User`
  - use a seeded user account or register a new one
- Check `doc/DEMO_DATA.md` for demo accounts and the demo card list to add in `/User/Cards`

## Public Pages
- Open `/`
- Open `/Product`
- Open `/Home/HowItWorks`
- Open `/Home/Faq`
- Open `/Home/Contact`
- Open `/Home/Sitemap`

## Authentication Flow
- Register a new user at `/Account/Register`
- If email delivery fails, use the fallback verification link shown on the login page
- Sign in at `/Account/Login`
- Test forgot password at `/Account/ResetPassword`
- If SMTP fails, use the fallback reset link shown on the login page

## Product Purchase Flow
- Open `/Product`
- Choose any product
- Open product detail
- Click buy
- If you do not have a saved demo card yet, open `/User/Cards` and add one from `doc/DEMO_DATA.md`
- At checkout:
  - use `Prepaid` with a saved demo card to complete payment immediately (select saved card or enter manually)
  - use `Postpaid` to create a pending bill
- Confirm the action in the on-screen modal

### PayPal Sandbox (Optional)
- Ensure `PayPal:ClientId` and `PayPal:Secret` are configured with **Sandbox REST API credentials** (see PayPal Developer → My Apps & Credentials → Sandbox).
- When redirected to PayPal Sandbox to approve payment, sign in with a *Sandbox Personal account* (buyer).
- Buyer email example is listed in `doc/DEMO_DATA.md` (password is available in PayPal Developer Dashboard).

## Postpaid Bill Flow
- Open `/User/Transaction`
- Find a pending postpaid bill
- Click `Pay Now`
- Choose Card or PayPal Sandbox
- If you choose Card, select a saved card (or enter card number, expiry, and CVV manually)
- Confirm payment in the modal
- Check that the transaction becomes `Paid`
- Open `Bill PDF` or `Print / Save Bill as PDF` to export the receipt

## Feedback Flow
- Buy a product successfully first
- Return to the purchased product detail page
- Submit feedback
- Re-open the same product to see your feedback
- Edit the feedback within 1 day
- Sign in as admin and reply from `/Admin/Feedbacks`

## User Area
- Open `/User/Dashboard`
- Check summary cards, overdue reminders, and latest pending bill
- Open `/User/Profile`
- Open `/User/Transaction`
- Open `/User/Cards`
- add, edit, and delete one saved demo card
- Open `/User/Dnd`
- Open `/User/CallerTune`
- Open `/Feedback`

## Admin Area
- Open `/Admin/Dashboard`
- Review totals, overdue bills, activity chart, recent transactions, and recent feedback
- Open `/Admin/Users`
- Open `/Admin/Transaction`
- Open `/Admin/Feedbacks`
- Open `/Admin/AuditLogs` (review recent admin changes)

## Fast Checks
- Notification bell:
  - unread badge disappears after opening the bell
- Transaction filters:
  - test keyword, status, type, and range filters
- Empty states:
  - pages should still look clean when there is no data
- Confirm modal:
  - checkout, postpaid pay, delete, and feedback actions should use the same confirm UI

## Demo Order
1. Open home and products
2. Register or login
3. Buy one prepaid product
4. Buy one postpaid product
5. Pay the postpaid bill
6. Leave feedback on a purchased product
7. Export one bill as PDF
8. Open user dashboard
9. Open admin dashboard

## Notes
- SMTP may use a fallback link on the login page if local email sending fails.
- Seeded data includes sample products, cards, transactions, and users for demo use.
- Manual demo cards can be added from `doc/DEMO_DATA.md`.
- Demo card balances are saved in the dashboard and deducted by the payment flow.
- Main transaction pages support quick filtering for easier searching.
