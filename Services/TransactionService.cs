using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Services;
using System.Text.RegularExpressions;

public class PaymentProcessResult
{
    public bool IsSuccess => Transaction != null && string.IsNullOrWhiteSpace(ErrorMessage);
    public string? ErrorMessage { get; init; }
    public Transaction? Transaction { get; init; }
}

public class TransactionService
{
    private readonly MobileRechargeDbContext _context;
    private const int PostpaidLockGraceDays = 2;

    public TransactionService(MobileRechargeDbContext context)
    {
        _context = context;
    }

    public Transaction CreatePostpaid(int productId, int userId, string phone)
    {
        var product = _context.Products.Find(productId);

        if (product == null)
            throw new Exception("Product does not exist.");

        var now = DateTime.Now;
        var lockThreshold = now.AddDays(-PostpaidLockGraceDays);

        var overduePostpaid = _context.Transactions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.Type == TransactionType.Postpaid &&
                x.Status == TransactionStatus.Pending &&
                !x.IsPaid &&
                x.DueDate != null &&
                x.DueDate < now)
            .OrderBy(x => x.DueDate)
            .FirstOrDefault();

        if (overduePostpaid != null)
        {
            if (overduePostpaid.DueDate < lockThreshold)
            {
                var user = _context.Users.FirstOrDefault(x => x.Id == userId && !x.IsDeleted && x.Role == "User");
                if (user != null)
                {
                    user.IsActive = false;
                    _context.SaveChanges();
                }

                throw new Exception("Account locked due to overdue postpaid bill.");
            }

            throw new Exception("Overdue postpaid bill exists.");
        }

        var hasPendingPostpaid = _context.Transactions
            .AsNoTracking()
            .Any(x =>
                x.UserId == userId &&
                x.Type == TransactionType.Postpaid &&
                x.Status == TransactionStatus.Pending &&
                !x.IsPaid &&
                (x.DueDate == null || x.DueDate >= now));

        if (hasPendingPostpaid)
        {
            throw new Exception("Pending postpaid bill already exists.");
        }

        var activeSale = GetActiveSale(productId, now);
        var amount = ProductSaleCalculator.GetEffectivePrice(product.Price, activeSale, now);

        var transaction = new Transaction
        {
            UserId = userId,
            PhoneNumber = phone,
            Amount = amount,
            Type = TransactionType.Postpaid,
            PaymentMethod = PaymentMethod.Postpaid,
            Status = TransactionStatus.Pending,
            CreatedAt = now,
            DueDate = now.AddDays(7),
            IsPaid = false,
            ProductId = productId
        };

        _context.Transactions.Add(transaction);
        _context.SaveChanges();

        return transaction;
    }

    public Transaction? GetById(int id)
    {
        return _context.Transactions.FirstOrDefault(x => x.Id == id);
    }

    public Transaction? GetUserTransactionById(int id, int userId)
    {
        return _context.Transactions
            .Include(x => x.Product)
            .FirstOrDefault(x => x.Id == id && x.UserId == userId);
    }

    public async Task<List<Transaction>> GetTransactionsAsync(
        int? userId,
        string? keyword,
        string? status = null,
        string? type = null,
        string? range = null)
    {
        var query = _context.Transactions
            .Include(x => x.User)
            .Include(x => x.Product)
            .AsQueryable();


        if (userId != null)
        {
            query = query.Where(x => x.UserId == userId);
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x =>
                x.PhoneNumber.Contains(normalizedKeyword) ||
                (x.User != null && (x.User.Name.Contains(normalizedKeyword) || x.User.Email.Contains(normalizedKeyword))) ||
                (x.Product != null && x.Product.Name.Contains(normalizedKeyword)));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<TransactionStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(type) &&
            !string.Equals(type, "all", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<TransactionType>(type, true, out var parsedType))
        {
            query = query.Where(x => x.Type == parsedType);
        }

        var now = DateTime.Now;
        var today = now.Date;

        switch ((range ?? "all").Trim().ToLowerInvariant())
        {
            case "today":
                query = query.Where(x => x.CreatedAt >= today);
                break;
            case "7days":
                query = query.Where(x => x.CreatedAt >= today.AddDays(-6));
                break;
            case "30days":
                query = query.Where(x => x.CreatedAt >= today.AddDays(-29));
                break;
            case "overdue":
                query = query.Where(x =>
                    x.Type == TransactionType.Postpaid &&
                    x.Status == TransactionStatus.Pending &&
                    x.DueDate != null &&
                    x.DueDate < now);
                break;
            case "duesoon":
                query = query.Where(x =>
                    x.Type == TransactionType.Postpaid &&
                    x.Status == TransactionStatus.Pending &&
                    x.DueDate != null &&
                    x.DueDate >= now &&
                    x.DueDate <= now.AddDays(2));
                break;
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public PaymentProcessResult ProcessPayment(
        int productId,
        int userId,
        string phone,
        string type,
        string? prepaidPaymentMethod,
        string? postpaidNationalId,
        string? postpaidBillingAddress,
        bool postpaidAgreeToTerms,
        bool postpaidAgreeToContract,
        string? cardNumber,
        string? cvv,
        string? expiry)
    {
        var product = _context.Products.Find(productId);
        if (product == null)
        {
            return new PaymentProcessResult
            {
                ErrorMessage = "Product does not exist."
            };
        }

        var now = DateTime.Now;
        var activeSale = GetActiveSale(productId, now);
        var amount = ProductSaleCalculator.GetEffectivePrice(product.Price, activeSale, now);

        var normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
        var sanitizedPhone = NormalizePhone(phone);
        var normalizedPrepaidMethod = (prepaidPaymentMethod ?? "card").Trim().ToLowerInvariant();

        if (normalizedType == "postpaid")
        {
            if (!postpaidAgreeToTerms || !postpaidAgreeToContract)
            {
                return new PaymentProcessResult
                {
                    ErrorMessage = "You must agree to the postpaid terms and contract before creating a bill."
                };
            }

            var lockThreshold = now.AddDays(-PostpaidLockGraceDays);

            var overduePostpaid = _context.Transactions
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.Type == TransactionType.Postpaid &&
                    x.Status == TransactionStatus.Pending &&
                    !x.IsPaid &&
                    x.DueDate != null &&
                    x.DueDate < now)
                .OrderBy(x => x.DueDate)
                .FirstOrDefault();

            if (overduePostpaid != null)
            {
                if (overduePostpaid.DueDate < lockThreshold)
                {
                    var user = _context.Users.FirstOrDefault(x => x.Id == userId && !x.IsDeleted && x.Role == "User");
                    if (user != null)
                    {
                        user.IsActive = false;
                        _context.SaveChanges();
                    }

                    return new PaymentProcessResult
                    {
                        ErrorMessage = "Your account has been locked due to an overdue postpaid bill."
                    };
                }

                return new PaymentProcessResult
                {
                    ErrorMessage = "You have an overdue postpaid bill. Please settle it before creating a new one."
                };
            }

            var hasPendingPostpaid = _context.Transactions
                .AsNoTracking()
                .Any(x =>
                    x.UserId == userId &&
                    x.Type == TransactionType.Postpaid &&
                    x.Status == TransactionStatus.Pending &&
                    !x.IsPaid &&
                    (x.DueDate == null || x.DueDate >= now));

            if (hasPendingPostpaid)
            {
                return new PaymentProcessResult
                {
                    ErrorMessage = "You already have a pending postpaid bill. Please settle it before creating a new one."
                };
            }

            var postpaidTransaction = new Transaction
            {
                UserId = userId,
                PhoneNumber = sanitizedPhone,
                Amount = amount,
                Type = TransactionType.Postpaid,
                PaymentMethod = PaymentMethod.Postpaid,
                Status = TransactionStatus.Pending,
                CreatedAt = now,
                DueDate = now.AddDays(7),
                IsPaid = false,
                PostpaidNationalId = string.IsNullOrWhiteSpace(postpaidNationalId) ? null : postpaidNationalId.Trim(),
                PostpaidBillingAddress = string.IsNullOrWhiteSpace(postpaidBillingAddress) ? null : postpaidBillingAddress.Trim(),
                PostpaidAgreedAt = now,
                PostpaidContractAgreedAt = now,
                ProductId = productId
            };

            _context.Transactions.Add(postpaidTransaction);
            _context.SaveChanges();

            return new PaymentProcessResult
            {
                Transaction = postpaidTransaction
            };
        }

        if (normalizedPrepaidMethod != "card" &&
            normalizedPrepaidMethod != "paypal")
        {
            return new PaymentProcessResult
            {
                ErrorMessage = "Invalid payment method."
            };
        }

        if (normalizedPrepaidMethod == "paypal")
        {
            var pendingTransaction = new Transaction
            {
                UserId = userId,
                PhoneNumber = sanitizedPhone,
                Amount = amount,
                Type = TransactionType.Prepaid,
                PaymentMethod = PaymentMethod.PayPalSandbox,
                Status = TransactionStatus.Pending,
                CreatedAt = now,
                IsPaid = false,
                ProductId = productId
            };

            _context.Transactions.Add(pendingTransaction);
            _context.SaveChanges();

            return new PaymentProcessResult
            {
                Transaction = pendingTransaction
            };
        }

        if (!TryParseExpiry(expiry, out var parsedExpiryDate))
        {
            return new PaymentProcessResult
            {
                ErrorMessage = "Invalid expiry value."
            };
        }

        if (parsedExpiryDate.Date < DateTime.Today)
        {
            return PersistFailedTransaction(productId, userId, sanitizedPhone, amount, "The card has expired.");
        }

        var matchedCard = FindMatchingCardForUser(userId, cardNumber, cvv, parsedExpiryDate);
        if (matchedCard == null)
        {
            return PersistFailedTransaction(productId, userId, sanitizedPhone, amount, "The card information does not match our records.");
        }

        if (matchedCard.Balance < amount)
        {
            return PersistFailedTransaction(productId, userId, sanitizedPhone, amount, "The card does not have enough balance.");
        }

        matchedCard.Balance -= amount;

        var transaction = new Transaction
        {
            UserId = userId,
            PhoneNumber = sanitizedPhone,
            Amount = amount,
            Type = TransactionType.Prepaid,
            PaymentMethod = PaymentMethod.Prepaid,
            Status = TransactionStatus.Success,
            CreatedAt = now,
            IsPaid = true,
            ProductId = productId
        };

        _context.Transactions.Add(transaction);
        TryActivateDataSubscription(product, sanitizedPhone, now);
        TryActivateCallerTuneSubscription(product, userId, now);
        _context.SaveChanges();

        return new PaymentProcessResult
        {
            Transaction = transaction
        };
    }

    public PaymentProcessResult SettlePostpaid(int transactionId, int userId, string? cardNumber, string? cvv, string? expiry)
    {
        var transaction = _context.Transactions
            .FirstOrDefault(x =>
                x.Id == transactionId &&
                x.UserId == userId &&
                x.Type == TransactionType.Postpaid &&
                x.Status == TransactionStatus.Pending);

        if (transaction == null)
        {
            return new PaymentProcessResult
            {
                ErrorMessage = "Pending postpaid bill not found."
            };
        }

        if (!TryParseExpiry(expiry, out var parsedExpiryDate))
        {
            return new PaymentProcessResult
            {
                ErrorMessage = "Invalid expiry value."
            };
        }

        if (parsedExpiryDate.Date < DateTime.Today)
        {
            return new PaymentProcessResult
            {
                ErrorMessage = "The card has expired."
            };
        }

        var matchedCard = FindMatchingCardForUser(userId, cardNumber, cvv, parsedExpiryDate);
        if (matchedCard == null)
        {
            return new PaymentProcessResult
            {
                ErrorMessage = "The card information does not match our records."
            };
        }

        if (matchedCard.Balance < transaction.Amount)
        {
            return new PaymentProcessResult
            {
                ErrorMessage = "The card does not have enough balance."
            };
        }

        matchedCard.Balance -= transaction.Amount;
        transaction.PaymentMethod = PaymentMethod.Prepaid;
        transaction.Status = TransactionStatus.Success;
        transaction.IsPaid = true;

        _context.SaveChanges();

        return new PaymentProcessResult
        {
            Transaction = transaction
        };
    }

    public void Save()
    {
        _context.SaveChanges();
    }

    private PaymentProcessResult PersistFailedTransaction(int productId, int userId, string phone, decimal amount, string message)
    {
        var transaction = new Transaction
        {
            UserId = userId,
            PhoneNumber = phone,
            Amount = amount,
            Type = TransactionType.Prepaid,
            PaymentMethod = PaymentMethod.Prepaid,
            Status = TransactionStatus.Failed,
            CreatedAt = DateTime.Now,
            IsPaid = false,
            ProductId = productId
        };

        _context.Transactions.Add(transaction);
        _context.SaveChanges();

        return new PaymentProcessResult
        {
            ErrorMessage = message,
            Transaction = transaction
        };
    }

    private Card? FindMatchingCardForUser(int userId, string? cardNumber, string? cvv, DateTime expiryDate)
    {
        var normalizedCardNumber = (cardNumber ?? string.Empty).Trim();
        var normalizedCvv = (cvv ?? string.Empty).Trim();

        var role = _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.Role)
            .FirstOrDefault();

        var cards = _context.Cards.AsQueryable();

        // Guest checkout has no saved wallet, so allow matching any demo card record.
        if (!string.Equals(role, "Guest", StringComparison.OrdinalIgnoreCase))
        {
            cards = cards.Where(x => x.UserId == userId);
        }

        return cards.FirstOrDefault(x =>
            x.CardNumber == normalizedCardNumber &&
            x.CVV == normalizedCvv &&
            x.ExpiryDate.Month == expiryDate.Month &&
            x.ExpiryDate.Year == expiryDate.Year);
    }

    public PaymentProcessResult CompleteSandboxPrepaid(int transactionId, int userId, bool success)
    {
        return new PaymentProcessResult
        {
            ErrorMessage = "Sandbox payment is no longer supported."
        };
    }

    public PaymentProcessResult CompletePayPalApproved(int transactionId, int userId, string? orderId, string? payerId)
    {
        var transaction = _context.Transactions
            .Include(x => x.Product)
            .FirstOrDefault(x =>
                x.Id == transactionId &&
                x.UserId == userId &&
                x.Type == TransactionType.Prepaid &&
                x.Status == TransactionStatus.Pending &&
                x.PaymentMethod == PaymentMethod.PayPalSandbox);

        if (transaction == null)
        {
            return new PaymentProcessResult
            {
                ErrorMessage = "Pending PayPal transaction not found."
            };
        }

        if (!string.IsNullOrWhiteSpace(orderId))
        {
            transaction.PaymentExternalId = orderId;
        }

        if (!string.IsNullOrWhiteSpace(payerId))
        {
            transaction.PaymentExternalPayerId = payerId;
        }

        var now = DateTime.Now;
        transaction.Status = TransactionStatus.Success;
        transaction.IsPaid = true;

        if (transaction.Product != null)
        {
            TryActivateDataSubscription(transaction.Product, transaction.PhoneNumber, now);
            TryActivateCallerTuneSubscription(transaction.Product, userId, now);
        }

        _context.SaveChanges();

        return new PaymentProcessResult
        {
            Transaction = transaction
        };
    }

    public void MarkPrepaidFailed(int transactionId, int userId, string reason)
    {
        var transaction = _context.Transactions.FirstOrDefault(x =>
            x.Id == transactionId &&
            x.UserId == userId &&
            x.Type == TransactionType.Prepaid &&
            x.Status == TransactionStatus.Pending);

        if (transaction == null)
        {
            return;
        }

        transaction.Status = TransactionStatus.Failed;
        transaction.IsPaid = false;
        _context.SaveChanges();
    }

    private static bool TryParseExpiry(string? expiry, out DateTime expiryDate)
    {
        expiryDate = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(expiry))
        {
            return false;
        }

        var parts = expiry.Split('/');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var month) ||
            !int.TryParse(parts[1], out var year) ||
            month is < 1 or > 12)
        {
            return false;
        }

        var fullYear = 2000 + year;
        expiryDate = new DateTime(fullYear, month, 1).AddMonths(1).AddDays(-1);
        return true;
    }

    private void TryActivateDataSubscription(Product product, string phoneNumber, DateTime now)
    {
        if (product.Type != ProductType.Data)
        {
            return;
        }

        phoneNumber = NormalizePhone(phoneNumber);
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return;
        }

        var validDays = GetValidDays(product);
        if (validDays <= 0)
        {
            return;
        }

        var subscription = _context.PhoneDataSubscriptions
            .FirstOrDefault(x => x.PhoneNumber == phoneNumber && x.ProductId == product.Id);

        if (subscription == null)
        {
            _context.PhoneDataSubscriptions.Add(new PhoneDataSubscription
            {
                PhoneNumber = phoneNumber,
                ProductId = product.Id,
                ActivatedAt = now,
                ExpiresAt = now.AddDays(validDays),
                CreatedAt = now
            });
            return;
        }

        var baseTime = subscription.ExpiresAt > now ? subscription.ExpiresAt : now;
        if (subscription.ExpiresAt <= now)
        {
            subscription.ActivatedAt = now;
        }

        subscription.ExpiresAt = baseTime.AddDays(validDays);
        subscription.UpdatedAt = now;
    }

    private void TryActivateCallerTuneSubscription(Product product, int userId, DateTime now)
    {
        if (product.Type != ProductType.CallerTune)
        {
            return;
        }

        var validDays = GetValidDays(product);
        if (validDays <= 0)
        {
            return;
        }

        var subscription = _context.CallerTuneSubscriptions
            .FirstOrDefault(x => x.UserId == userId);

        if (subscription == null)
        {
            _context.CallerTuneSubscriptions.Add(new CallerTuneSubscription
            {
                UserId = userId,
                ActivatedAt = now,
                ExpiresAt = now.AddDays(validDays),
                CreatedAt = now
            });
            return;
        }

        var baseTime = subscription.ExpiresAt > now ? subscription.ExpiresAt : now;
        if (subscription.ExpiresAt <= now)
        {
            subscription.ActivatedAt = now;
        }

        subscription.ExpiresAt = baseTime.AddDays(validDays);
        subscription.UpdatedAt = now;
    }

    private static int GetValidDays(Product product)
    {
        if (product.ValidDays is int days && days > 0)
        {
            return days;
        }

        return ExtractValidDays(product.Name) ?? ExtractValidDays(product.Description) ?? 0;
    }

    private static int? ExtractValidDays(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text.Trim();

        var match = Regex.Match(normalized, @"\/\s*(\d+)\s*(day|days|night|nights)\b", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var days))
        {
            return days;
        }

        match = Regex.Match(normalized, @"\bday\s*x\s*(\d+)\b", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out days))
        {
            return days;
        }

        match = Regex.Match(normalized, @"\bx\s*(\d+)\b", RegexOptions.IgnoreCase);
        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out days) &&
            normalized.Contains("mb", StringComparison.OrdinalIgnoreCase))
        {
            return days;
        }

        return null;
    }

    private static string NormalizePhone(string? phone)
    {
        var trimmed = (phone ?? string.Empty).Trim();
        return Regex.Replace(trimmed, @"[^\d]", string.Empty);
    }

    private ProductSale? GetActiveSale(int productId, DateTime now)
    {
        return _context.ProductSales
            .AsNoTracking()
            .Where(x =>
                x.ProductId == productId &&
                x.SaleType != ProductSaleType.None &&
                x.SaleValue > 0 &&
                (x.StartAt == null || x.StartAt <= now) &&
                (x.EndAt == null || x.EndAt >= now))
            .OrderByDescending(x => x.StartAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }
}
