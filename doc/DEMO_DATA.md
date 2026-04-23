# Demo Data

This project runs in demo mode with seeded users, products, transactions, and demo cards.

## Seeded Accounts

All seeded accounts use password: `123`

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@gmail.com` | `123` |
| User | `user1@gmail.com` | `123` |
| User | `user2@gmail.com` | `123` |
| User | `user3@gmail.com` | `123` |
| User | `user4@gmail.com` | `123` |
| User | `user5@gmail.com` | `123` |

## Demo Cards (Manual Add List)

These cards are seeded into the database on first run. You can also add them manually at `/User/Cards`.

Expiry input format is `MM/YY`.

| Card Number | CVV | Expiry | Initial Balance (VND) |
| --- | --- | --- | --- |
| `6011223344557001` | `321` | `12/28` | `450,000` |
| `6011223344557002` | `654` | `11/29` | `780,000` |
| `6011223344557003` | `147` | `10/30` | `1,050,000` |
| `4555666677778001` | `258` | `09/29` | `1,400,000` |
| `4555666677778002` | `369` | `08/30` | `1,850,000` |

## Notes

- Seeded users may already have demo cards in `/User/Cards`.
- Payments deduct balance from the selected saved card (demo only).
- Guest topup (`/GuestCheckout`) validates against the same card records (manual entry) and also supports PayPal Sandbox.
- Postpaid checkout creates a pending bill, then you can settle it later from `/User/Transaction` using Card or PayPal Sandbox.

## PayPal Sandbox (Test Buyer)

For PayPal Sandbox testing, use a *Sandbox Personal account* (buyer) created in the PayPal Developer Dashboard.

- Buyer email: `sb-fatcq50787209@personal.example.com`
- Password: get it from PayPal Developer → Dashboard → Sandbox → Accounts → select the account → View details.
