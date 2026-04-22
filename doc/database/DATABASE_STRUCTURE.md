# Database Structure

## Main Tables
- `Users`
- `Products`
- `Sales`
- `ProductSales`
- `Transactions`
- `Feedbacks`
- `Cards`
- `DndSettings`
- `DndNumbers`
- `CallerTunes`
- `PhoneDataSubscriptions`

## Core Links
- `Users` -> `Transactions`
- `Users` -> `Feedbacks`
- `Users` -> `Cards`
- `Users` -> `DndSettings`
- `Users` -> `CallerTunes`
- `Products` -> `Transactions`
- `Products` -> `Feedbacks`
- `Products` -> `ProductSales`
- `Transactions` -> `Feedbacks`
- `Products` -> `PhoneDataSubscriptions`
- `DndSettings` -> `DndNumbers`

## Important Notes
- `Users.Email` is unique.
- `Users.NationalId` and `Users.BillingAddress` store the latest billing profile (editable by user/admin).
- `Transactions.ProductId` is optional.
- Postpaid fields are stored per transaction:
  - `Transactions.PostpaidNationalId`
  - `Transactions.PostpaidBillingAddress`
  - `Transactions.PostpaidAgreedAt`
- `Feedbacks.UserId`, `Feedbacks.ProductId`, and `Feedbacks.TransactionId` are optional.
- Enum values are stored as strings for `Product.Type`, `Transaction.Type`, `Transaction.Status`, `Transaction.PaymentMethod`, and `DndSettings.Mode`.
