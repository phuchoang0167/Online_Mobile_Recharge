# Route Map

## Public
- `/`
- `/Product`
- `/Product/Detail/{id}`
- `/Phone/Select`
- `/GuestCheckout?productId={id}` (guest topup checkout)
- `/GuestCheckout/Success/{id}`
- `/GuestPayment/PayPal/{id}` (guest PayPal sandbox start)
- `/GuestPayment/PayPalReturn` (guest PayPal return)
- `/GuestPayment/PayPalCancel` (guest PayPal cancel)
- `/Home/About`
- `/Home/HowItWorks`
- `/Home/Faq`
- `/Home/Contact`
- `/Home/Terms`
- `/Home/Privacy`
- `/Home/Sitemap`

## Authentication
- `/Account/Login`
- `/Account/Register`
- `/Account/ResetPassword`
- `/Account/ResetPasswordConfirm`
- `/Account/VerifyEmail`

## User Area
- `/User/Dashboard`
- `/User/Profile`
- `/User/Transaction`
- `/User/Transaction/Pay/{id}`
- `/User/Transaction/Invoice/{id}`
- `/User/Cards`
- `/User/Dnd`
- `/User/CallerTune`
- `/Feedback`

## Payment (Redirect Flows)
- `/Payment/PayPal/{id}` (PayPal sandbox start for prepaid/postpaid)
- `/Payment/PayPalReturn` (PayPal return)
- `/Payment/PayPalCancel` (PayPal cancel)

## Admin Area
- `/Admin/Dashboard`
- `/Admin/Users`
- `/Admin/Transaction`
- `/Admin/Feedbacks`
- `/Admin/AuditLogs`

## Notes
- Some actions are `POST` only and are triggered by forms or buttons.
- Role-based pages require login and the correct session/role.
