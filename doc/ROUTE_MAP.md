# Route Map

## Public
- `/`
- `/Product`
- `/Product/Detail/{id}`
- `/Phone/Select`
- `/GuestCheckout?productId={id}` (guest topup checkout)
- `/GuestCheckout/Success/{id}`
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

## Admin Area
- `/Admin/Dashboard`
- `/Admin/Users`
- `/Admin/Transaction`
- `/Admin/Feedbacks`

## Notes
- Some actions are `POST` only and are triggered by forms or buttons.
- Role-based pages require login and the correct session/role.
