# Project Context

## Overall Architecture
- ASP.NET Core MVC + EF Core + SQL Server.
- Entry point: `Program.cs`.
- Layers: `Controllers` -> `Services` -> `DbContext`/`Entities` -> `Views`.
- Auth: cookie auth + session + custom role filters.

## Module Relationships
- `AccountController` -> auth, register, verify email, reset password.
- `Product` + `Checkout` + `TransactionService` -> browse, buy, prepaid/postpaid flow.
- `UserTransaction` + `AdminTransaction` -> same transaction domain, different role scope.
- `FeedbackService` -> product reviews + support messages.
- `NotificationService` -> bell data from transactions, feedback, DND, caller tune.
- `UserController` / `AdminController` -> dashboard aggregation by role.
- `DbInitializer` -> demo users, cards, transactions, products, feedback.
- `HomeController` -> public pages, FAQ, policies, sitemap, contact flow.

## Main Domain Model
- `User` owns `Cards`, `Transactions`, `Feedbacks`.
- `Transaction` links `User` + optional `Product`.
- `Feedback` links optional `User`, `Product`, `Transaction`.
- `DndSetting` / `DndNumber` and `CallerTune` are user-side telecom add-ons.

## Refactor Targets
- Duplicate dashboard/query aggregation in controllers -> move to dedicated dashboard/query services.
- Controllers still contain business logic -> move more logic into services/use-case methods.
- `ViewBag` is used heavily -> replace with typed view models.
- Enum file is grouped in one file -> split by enum for clarity.
- Migration naming/history is noisy -> rename/clean for maintainability.
- `DbInitializer` is growing large -> split by domain seeders.
- Auth/session/role checks can be unified further into cleaner auth policies.
- Static content and diagrams should stay aligned with routes when new pages are added.
