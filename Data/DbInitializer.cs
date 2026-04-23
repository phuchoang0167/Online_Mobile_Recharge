using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;
using Online_Mobile_Recharge.Services;
using System.Text.RegularExpressions;

public static class DbInitializer
{
    public static void Seed(MobileRechargeDbContext context)
    {
        SeedUsers(context);
        SeedProducts(context);
        SeedSales(context);
        SeedProductSales(context);
        SeedCards(context);
        SeedTransactions(context);
        SeedFeedbacks(context);
        SeedAdminAuditLogs(context);
        EnsureLegacyUsersRemainVerified(context);
    }

    private static void SeedAdminAuditLogs(MobileRechargeDbContext context)
    {
        if (context.AdminAuditLogs.Any())
        {
            return;
        }

        var adminId = context.Users
            .AsNoTracking()
            .Where(x => x.Role == "Admin" && !x.IsDeleted)
            .Select(x => (int?)x.Id)
            .FirstOrDefault();

        if (adminId == null)
        {
            return;
        }

        var now = DateTime.Now;
        var sample = new List<AdminAuditLog>
        {
            new()
            {
                AdminUserId = adminId.Value,
                EntityType = "Product",
                EntityId = 1,
                Action = "Update",
                Summary = "Sample audit log: updated product flags.",
                OldDataJson = null,
                NewDataJson = "{\"IsTop\":true,\"IsSpecial\":false}",
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                AdminUserId = adminId.Value,
                EntityType = "Sale",
                EntityId = 1,
                Action = "Create",
                Summary = "Sample audit log: created a sale campaign.",
                OldDataJson = null,
                NewDataJson = "{\"SaleType\":\"Percent\",\"SaleValue\":10}",
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                AdminUserId = adminId.Value,
                EntityType = "User",
                EntityId = 2,
                Action = "Update",
                Summary = "Sample audit log: locked user with a reason.",
                OldDataJson = null,
                NewDataJson = "{\"IsActive\":false,\"LastLockReason\":\"Overdue postpaid\"}",
                CreatedAt = now.AddHours(-8)
            }
        };

        context.AdminAuditLogs.AddRange(sample);
        context.SaveChanges();
    }

    private static void SeedSales(MobileRechargeDbContext context)
    {
        if (context.Sales.Any())
        {
            return;
        }

        var now = DateTime.Now;
        var sales = new List<Sale>
        {
            new()
            {
                Name = "Flash Sale 10%",
                Description = "Sale campaign (active).",
                SaleType = ProductSaleType.Percent,
                SaleValue = 10,
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(7),
                IsEnabled = true,
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                Name = "Weekend Deal 20K",
                Description = "Sale campaign (upcoming).",
                SaleType = ProductSaleType.FixedAmount,
                SaleValue = 20000,
                StartAt = now.AddDays(2),
                EndAt = now.AddDays(4),
                IsEnabled = true,
                CreatedAt = now
            },
            new()
            {
                Name = "Old Promo 5%",
                Description = "Sale campaign (expired).",
                SaleType = ProductSaleType.Percent,
                SaleValue = 5,
                StartAt = now.AddDays(-30),
                EndAt = now.AddDays(-20),
                IsEnabled = false,
                CreatedAt = now.AddDays(-30)
            }
        };

        context.Sales.AddRange(sales);
        context.SaveChanges();
    }

    private static void SeedProductSales(MobileRechargeDbContext context)
    {
        if (context.ProductSales.Any())
        {
            return;
        }

        var products = context.Products
            .OrderBy(x => x.Id)
            .Take(4)
            .ToList();

        if (!products.Any())
        {
            return;
        }

        var now = DateTime.Now;
        var demoProductSales = new List<ProductSale>();

        if (products.Count >= 1)
        {
            demoProductSales.Add(new ProductSale
            {
                ProductId = products[0].Id,
                SaleType = ProductSaleType.Percent,
                SaleValue = 15,
                StartAt = now.AddHours(-2),
                EndAt = now.AddDays(5),
                CreatedAt = now.AddHours(-2)
            });
        }

        if (products.Count >= 2)
        {
            demoProductSales.Add(new ProductSale
            {
                ProductId = products[1].Id,
                SaleType = ProductSaleType.FixedAmount,
                SaleValue = 10000,
                StartAt = now.AddDays(1),
                EndAt = now.AddDays(8),
                CreatedAt = now
            });
        }

        if (products.Count >= 3)
        {
            demoProductSales.Add(new ProductSale
            {
                ProductId = products[2].Id,
                SaleType = ProductSaleType.FixedAmount,
                SaleValue = 20000,
                StartAt = now.AddDays(-10),
                EndAt = now.AddDays(-2),
                CreatedAt = now.AddDays(-10)
            });
        }

        if (products.Count >= 4)
        {
            demoProductSales.Add(new ProductSale
            {
                ProductId = products[3].Id,
                SaleType = ProductSaleType.None,
                SaleValue = 0,
                StartAt = null,
                EndAt = null,
                CreatedAt = now
            });
        }

        context.ProductSales.AddRange(demoProductSales);
        context.SaveChanges();
    }

    private static void SeedUsers(MobileRechargeDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var rng = new Random(20260417);
        var users = new List<User>();

        static string Pick(Random random, string[] source) => source[random.Next(0, source.Length)];

        var familyNames = new[]
        {
            "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý"
        };

        var middleNames = new[]
        {
            "Văn", "Thị", "Minh", "Thanh", "Quang", "Hữu", "Đức", "Gia", "Bảo", "Khánh", "Ngọc", "Anh", "Tuấn", "Hải", "Thảo", "Phương"
        };

        var givenNames = new[]
        {
            "An", "Bình", "Chi", "Dũng", "Dương", "Giang", "Hà", "Hiếu", "Hùng", "Hương", "Khang", "Lan", "Linh", "Long", "Mai", "Nam",
            "Nhi", "Nhung", "Phát", "Phúc", "Quân", "Sơn", "Tâm", "Thành", "Thảo", "Thịnh", "Trang", "Trí", "Trung", "Tú", "Uyên", "Vinh"
        };

        string BuildFullName()
        {
            var family = Pick(rng, familyNames);
            var middle = Pick(rng, middleNames);
            var given = Pick(rng, givenNames);
            return $"{family} {middle} {given}";
        }

        var admin = new User
        {
            Name = "Quản trị hệ thống",
            Email = "admin@gmail.com",
            Password = string.Empty,
            Role = "Admin",
            PhoneNumber = "0900000000",
            EmailVerified = true,
            CreatedAt = now
        };
        admin.Password = PasswordHelper.HashPassword(admin, "123");
        users.Add(admin);

        var initialUsers = new (string Name, string Email, string Phone)[]
        {
            ("Nguyễn Minh Anh", "minhanh.nguyen@gmail.com", "0900000001"),
            ("Trần Quang Huy", "quanghuy.tran@gmail.com", "0900000002"),
            ("Lê Thị Lan", "thilan.le@gmail.com", "0900000003"),
            ("Phạm Đức Long", "duclong.pham@gmail.com", "0900000004"),
            ("Hoàng Ngọc Mai", "ngocmai.hoang@gmail.com", "0900000005")
        };

        for (var i = 0; i < initialUsers.Length; i++)
        {
            var entry = initialUsers[i];
            var user = new User
            {
                Name = entry.Name,
                Email = entry.Email,
                Password = string.Empty,
                Role = "User",
                PhoneNumber = entry.Phone,
                EmailVerified = true,
                CreatedAt = now.AddDays(-(i + 1) * 2)
            };
            user.Password = PasswordHelper.HashPassword(user, "123");
            users.Add(user);
        }

        // Additional demo users for QA (varied verification/active states and profile completeness).
        for (var i = 1; i <= 80; i++)
        {
            var phone = $"091{(1000000 + i):0000000}"; // 10 digits
            var createdAt = now.AddDays(-rng.Next(0, 365)).AddMinutes(-rng.Next(0, 24 * 60));
            var emailVerified = rng.NextDouble() < 0.85;
            var isActive = rng.NextDouble() < 0.95;
            var isDeleted = rng.NextDouble() < 0.03;

            var user = new User
            {
                Name = BuildFullName(),
                Email = $"demo.user{i:000}@example.com",
                Password = string.Empty,
                Role = "User",
                PhoneNumber = phone,
                NationalId = null,
                BillingAddress = null,
                EmailVerified = emailVerified,
                IsActive = isActive,
                IsDeleted = isDeleted,
                CreatedAt = createdAt
            };
            user.Password = PasswordHelper.HashPassword(user, "123");
            users.Add(user);
        }

        // Demo guest user used by guest checkout flows (not allowed to login).
        var guest = new User
        {
            Name = "Khách 0909999999",
            Email = "guest+0909999999@guest.local",
            Password = string.Empty,
            Role = "Guest",
            PhoneNumber = "0909999999",
            EmailVerified = true,
            CreatedAt = now
        };
        guest.Password = PasswordHelper.HashPassword(guest, Guid.NewGuid().ToString("N"));
        users.Add(guest);

        context.Users.AddRange(users);
        context.SaveChanges();
    }

    private static void SeedProducts(MobileRechargeDbContext context)
    {
        var existingNames = context.Products
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sampleProducts = BuildSampleProducts();
        EnsureValidityDays(sampleProducts);

        var productsToAdd = sampleProducts
            .Where(x => !existingNames.Contains(x.Name))
            .ToList();

        if (productsToAdd.Any())
        {
            context.Products.AddRange(productsToAdd);
        }

        var existingDataProducts = context.Products
            .Where(x => x.Type == ProductType.Data && (x.ValidDays == null || x.ValidDays <= 0))
            .ToList();

        if (existingDataProducts.Any())
        {
            var lookup = sampleProducts
                .Where(x => x.Type == ProductType.Data)
                .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var product in existingDataProducts)
            {
                if (lookup.TryGetValue(product.Name, out var sample) && sample.ValidDays != null && sample.ValidDays > 0)
                {
                    product.ValidDays = sample.ValidDays;
                    continue;
                }

                product.ValidDays = product.ValidDays ?? TryExtractValidDays(product.Name) ?? TryExtractValidDays(product.Description);
            }
        }

        if (productsToAdd.Any() || existingDataProducts.Any())
        {
            context.SaveChanges();
        }
    }

    private static void EnsureValidityDays(List<Product> products)
    {
        foreach (var product in products)
        {
            if (product.Type != ProductType.Data)
            {
                continue;
            }

            if (product.ValidDays != null && product.ValidDays > 0)
            {
                continue;
            }

            product.ValidDays = TryExtractValidDays(product.Name) ?? TryExtractValidDays(product.Description);
        }
    }

    private static int? TryExtractValidDays(string? text)
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

    private static void SeedCards(MobileRechargeDbContext context)
    {
        var users = context.Users
            .Where(x => x.Role == "User" && !x.IsDeleted)
            .OrderBy(x => x.Id)
            .ToList();

        var cards = new List<Card>();

        foreach (var user in users)
        {
            cards.AddRange(BuildDemoCardsForUser(user.Id));
        }

        var seedUserId = users.FirstOrDefault()?.Id;
        if (seedUserId != null)
        {
            foreach (var info in DemoCardCatalog.GetAll())
            {
                cards.Add(new Card
                {
                    UserId = seedUserId.Value,
                    CardNumber = info.CardNumber,
                    CVV = info.CVV,
                    Balance = info.InitialBalance,
                    Price = 0,
                    ExpiryDate = new DateTime(info.ExpiryYear, info.ExpiryMonth, 1)
                        .AddMonths(1)
                        .AddDays(-1)
                });
            }
        }

        if (!cards.Any())
        {
            return;
        }

        var existingCardNumbers = context.Cards
            .Select(x => x.CardNumber)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cardsToAdd = cards
            .Where(x => !existingCardNumbers.Contains(x.CardNumber))
            .ToList();

        if (!cardsToAdd.Any())
        {
            return;
        }

        context.Cards.AddRange(cardsToAdd);
        context.SaveChanges();
    }

    public static void EnsureDemoCardsForUser(MobileRechargeDbContext context, User user)
    {
        if (user.Role != "User" || user.IsDeleted)
        {
            return;
        }

        var existingCardNumbers = context.Cards
            .Where(x => x.UserId == user.Id)
            .Select(x => x.CardNumber)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cardsToAdd = BuildDemoCardsForUser(user.Id)
            .Where(x => !existingCardNumbers.Contains(x.CardNumber))
            .ToList();

        if (!cardsToAdd.Any())
        {
            return;
        }

        context.Cards.AddRange(cardsToAdd);
        context.SaveChanges();
    }

    private static void SeedTransactions(MobileRechargeDbContext context)
    {
        if (context.Transactions.Any())
        {
            return;
        }

        var rng = new Random(20260417);
        var users = context.Users.Where(x => x.Role == "User").OrderBy(x => x.Id).ToList();
        var guestUser = context.Users.FirstOrDefault(x => x.Role == "Guest");
        var products = context.Products.OrderBy(x => x.Id).ToList();
        if (!users.Any() || !products.Any())
        {
            return;
        }

        var now = DateTime.Now;
        var transactions = new List<Transaction>(capacity: 1000);

        // Ensure a few predictable baseline transactions for the first demo users.
        foreach (var user in users.Take(5))
        {
            var basePhone = user.PhoneNumber;
            var successProduct = products[(user.Id - 1) % products.Count];
            var failedProduct = products[(user.Id + 1) % products.Count];
            var pendingProduct = products[(user.Id + 2) % products.Count];

            transactions.Add(new Transaction
            {
                UserId = user.Id,
                PhoneNumber = basePhone,
                Amount = successProduct.Price,
                Type = TransactionType.Prepaid,
                Status = TransactionStatus.Success,
                PaymentMethod = PaymentMethod.Prepaid,
                ProductId = successProduct.Id,
                CreatedAt = now.AddDays(-10 - user.Id),
                IsPaid = true
            });

            transactions.Add(new Transaction
            {
                UserId = user.Id,
                PhoneNumber = basePhone,
                Amount = failedProduct.Price,
                Type = TransactionType.Prepaid,
                Status = TransactionStatus.Failed,
                PaymentMethod = PaymentMethod.Prepaid,
                ProductId = failedProduct.Id,
                CreatedAt = now.AddDays(-4 - user.Id),
                IsPaid = false
            });

            transactions.Add(new Transaction
            {
                UserId = user.Id,
                PhoneNumber = basePhone,
                Amount = pendingProduct.Price,
                Type = TransactionType.Postpaid,
                Status = TransactionStatus.Pending,
                PaymentMethod = PaymentMethod.Postpaid,
                ProductId = pendingProduct.Id,
                CreatedAt = now.AddDays(-2 - user.Id),
                DueDate = now.AddDays((user.Id % 2 == 0) ? 1 : -1),
                IsPaid = false,
                PostpaidNationalId = null,
                PostpaidBillingAddress = null,
                PostpaidAgreedAt = now.AddDays(-2 - user.Id).AddMinutes(5)
            });
        }

        // Generate ~1000 transactions with mixed scenarios for QA.
        while (transactions.Count < 1000)
        {
            var useGuest = guestUser != null && rng.NextDouble() < 0.08;
            var user = useGuest ? guestUser! : users[rng.Next(users.Count)];

            var createdAt = now
                .AddDays(-rng.Next(0, 180))
                .AddMinutes(-rng.Next(0, 24 * 60));

            var isPostpaid = rng.NextDouble() < 0.30;
            var type = isPostpaid ? TransactionType.Postpaid : TransactionType.Prepaid;
            var paymentMethod = isPostpaid ? PaymentMethod.Postpaid : PaymentMethod.Prepaid;

            var statusRoll = rng.NextDouble();
            var status = statusRoll < 0.75
                ? TransactionStatus.Success
                : (statusRoll < 0.90 ? TransactionStatus.Failed : TransactionStatus.Pending);

            var product = products[rng.Next(products.Count)];
            var amount = product.Price;

            // A few "odd" amounts to test edge cases, without going negative.
            if (rng.NextDouble() < 0.05)
            {
                amount = Math.Max(1000, amount + rng.Next(-5000, 5001));
            }

            var phoneNumber = user.PhoneNumber;
            if (rng.NextDouble() < 0.20)
            {
                phoneNumber = $"09{rng.Next(10000000, 99999999)}";
            }

            var transaction = new Transaction
            {
                UserId = user.Id,
                PhoneNumber = phoneNumber,
                Amount = amount,
                Type = type,
                Status = status,
                PaymentMethod = paymentMethod,
                ProductId = (rng.NextDouble() < 0.97) ? product.Id : null,
                CreatedAt = createdAt,
                IsPaid = status == TransactionStatus.Success
            };

            if (type == TransactionType.Postpaid)
            {
                transaction.PostpaidNationalId = null;
                transaction.PostpaidBillingAddress = null;

                if (rng.NextDouble() < 0.90)
                {
                    transaction.PostpaidAgreedAt = createdAt.AddMinutes(rng.Next(1, 30));
                }

                if (status == TransactionStatus.Pending)
                {
                    transaction.DueDate = createdAt.AddDays(rng.Next(-5, 11));
                    transaction.IsPaid = false;
                }
                else if (status == TransactionStatus.Success && rng.NextDouble() < 0.15)
                {
                    // Some postpaid transactions paid after due date (historical) or early (future) for reporting checks.
                    transaction.DueDate = createdAt.AddDays(rng.Next(-10, 10));
                    transaction.IsPaid = true;
                }
            }

            transactions.Add(transaction);
        }

        context.Transactions.AddRange(transactions);
        context.SaveChanges();
    }

    private static void SeedFeedbacks(MobileRechargeDbContext context)
    {
        if (context.Feedbacks.Any())
        {
            return;
        }

        var successfulTransactions = context.Transactions
            .Where(x => x.Status == TransactionStatus.Success && x.ProductId != null)
            .OrderBy(x => x.Id)
            .Take(3)
            .ToList();

        var feedbacks = new List<Feedback>();

        foreach (var transaction in successfulTransactions)
        {
            var user = context.Users.First(x => x.Id == transaction.UserId);
            feedbacks.Add(new Feedback
            {
                Category = FeedbackService.ProductReviewCategory,
                Name = user.Name,
                Email = user.Email,
                Message = "The product is reliable, payment is fast, and the interface is easy to follow.",
                UserId = user.Id,
                ProductId = transaction.ProductId,
                TransactionId = transaction.Id,
                CreatedAt = DateTime.Now.AddDays(-1)
            });
        }

        feedbacks.Add(new Feedback
        {
            Category = FeedbackService.SupportCategory,
            Name = "Guest Contact",
            Email = "guest@example.com",
            Message = "I would like more guidance about the postpaid payment process.",
            CreatedAt = DateTime.Now.AddHours(-10)
        });

        context.Feedbacks.AddRange(feedbacks);
        context.SaveChanges();
    }

    private static void EnsureLegacyUsersRemainVerified(MobileRechargeDbContext context)
    {
        var demoEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "admin@gmail.com",
            "user1@gmail.com",
            "user2@gmail.com",
            "user3@gmail.com",
            "user4@gmail.com",
            "user5@gmail.com"
        };

        var legacyUsers = context.Users
            .Where(x =>
                !x.EmailVerified &&
                demoEmails.Contains(x.Email))
            .ToList();

        if (!legacyUsers.Any())
        {
            return;
        }

        foreach (var user in legacyUsers)
        {
            user.EmailVerified = true;
        }

        context.SaveChanges();
    }

    private static List<Card> BuildDemoCardsForUser(int userId)
    {
        return new List<Card>
        {
            new()
            {
                UserId = userId,
                CardNumber = $"520000000000{(1000 + userId):0000}",
                CVV = ((200 + userId) % 1000).ToString("000"),
                Balance = 400000 + (userId * 120000),
                Price = 0,
                ExpiryDate = DateTime.Today.AddYears(2).AddMonths(userId % 6)
            },
            new()
            {
                UserId = userId,
                CardNumber = $"430000000000{(2000 + userId):0000}",
                CVV = ((500 + userId) % 1000).ToString("000"),
                Balance = 850000 + (userId * 180000),
                Price = 0,
                ExpiryDate = DateTime.Today.AddYears(3).AddMonths(userId % 8)
            }
        };
    }

    private static List<Product> BuildSampleProducts()
    {
        var dataProducts = new List<Product>
        {
            new() { Name = "Data 1GB / 1 day", Description = "A one-day data pack for urgent browsing and messaging.", Price = 10000, Type = ProductType.Data },
            new() { Name = "Data 2GB / 3 days", Description = "A light data package for basic needs over 3 days.", Price = 20000, Type = ProductType.Data, IsTop = true },
            new() { Name = "Data 3GB / 3 days", Description = "A practical short-term data choice for maps, chat, and social apps.", Price = 25000, Type = ProductType.Data },
            new() { Name = "Data 5GB / 7 days", Description = "Works well for study, streaming, and social apps.", Price = 50000, Type = ProductType.Data, IsSpecial = true },
            new() { Name = "Data 6GB / 7 days", Description = "Balanced weekly data for daily work and entertainment.", Price = 55000, Type = ProductType.Data },
            new() { Name = "Data 8GB / 10 days", Description = "A flexible package for medium mobile traffic over ten days.", Price = 70000, Type = ProductType.Data },
            new() { Name = "Data 10GB / 15 days", Description = "A stable data option for frequent mobile use.", Price = 90000, Type = ProductType.Data, IsTop = true },
            new() { Name = "Data 12GB / 15 days", Description = "A stronger half-month package for video, music, and navigation.", Price = 100000, Type = ProductType.Data },
            new() { Name = "Data 15GB / 30 days", Description = "A monthly data pack for light streaming and remote work.", Price = 120000, Type = ProductType.Data },
            new() { Name = "Data 20GB / 30 days", Description = "A monthly package for both work and entertainment.", Price = 150000, Type = ProductType.Data, IsSpecial = true },
            new() { Name = "Data 25GB / 30 days", Description = "A larger monthly allowance for users who are online every day.", Price = 170000, Type = ProductType.Data },
            new() { Name = "Data 30GB / 30 days", Description = "Suitable for streaming, meetings, and regular app downloads.", Price = 190000, Type = ProductType.Data, IsTop = true },
            new() { Name = "Night Data 5GB / 7 nights", Description = "A value pack for night-time browsing, streaming, and downloads.", Price = 30000, Type = ProductType.Data },
            new() { Name = "Night Data 15GB / 30 nights", Description = "A monthly night data pack for heavy off-peak use.", Price = 70000, Type = ProductType.Data },
            new() { Name = "Social Data 7GB / 30 days", Description = "Built for messaging, social media, and basic image sharing.", Price = 65000, Type = ProductType.Data },
            new() { Name = "Streaming Data 12GB / 30 days", Description = "Extra data for music and video streaming across the month.", Price = 95000, Type = ProductType.Data },
            new() { Name = "Gaming Data 10GB / 30 days", Description = "Lower-latency focused data for mobile gaming sessions.", Price = 105000, Type = ProductType.Data },
            new() { Name = "Business Data 18GB / 30 days", Description = "A practical plan for work chat, email, and video meetings.", Price = 145000, Type = ProductType.Data },
            new() { Name = "Student Data 9GB / 30 days", Description = "Designed for online classes, reading, and collaboration tools.", Price = 75000, Type = ProductType.Data },
            new() { Name = "Family Data 35GB / 30 days", Description = "A larger monthly data option for shared family usage patterns.", Price = 220000, Type = ProductType.Data, IsSpecial = true },
            new() { Name = "Weekend Data 4GB / 2 days", Description = "Short high-value data for weekend browsing and travel.", Price = 18000, Type = ProductType.Data },
            new() { Name = "Travel Data 10GB / 5 days", Description = "A travel-friendly data pack for maps, booking, and updates.", Price = 60000, Type = ProductType.Data },
            new() { Name = "Flexi Data 14GB / 21 days", Description = "A middle-ground plan between weekly and monthly packages.", Price = 115000, Type = ProductType.Data },
            new() { Name = "Unlimited Social / 30 days", Description = "Keep messaging and social feeds active across the month.", Price = 89000, Type = ProductType.Data, IsTop = true },
            new() { Name = "Unlimited Video / 30 days", Description = "A content-first package for frequent mobile video viewers.", Price = 179000, Type = ProductType.Data, IsSpecial = true },
            new() { Name = "Voice + Data 5GB / 30 days", Description = "Includes basic call support together with monthly data.", Price = 99000, Type = ProductType.Data },
            new() { Name = "Voice + Data 12GB / 30 days", Description = "A combined plan for callers who still need solid monthly data.", Price = 149000, Type = ProductType.Data },
            new() { Name = "Workday Data 2GB / day x 7", Description = "A daily allocation style package for active weekday users.", Price = 68000, Type = ProductType.Data },
            new() { Name = "Daily Booster 500MB x 30", Description = "A top-up style rolling data pack for steady monthly use.", Price = 95000, Type = ProductType.Data },
            new() { Name = "Heavy Data 50GB / 30 days", Description = "A premium monthly package for very high mobile data demand.", Price = 279000, Type = ProductType.Data, IsSpecial = true }
        };

        var cardProducts = new List<Product>
        {
            new() { Name = "Topup 10K", Description = "Recharge your account with 10,000 VND.", Price = 10000, Type = ProductType.Card },
            new() { Name = "Topup 20K", Description = "Recharge your account with 20,000 VND.", Price = 20000, Type = ProductType.Card },
            new() { Name = "Topup 30K", Description = "Recharge your account with 30,000 VND.", Price = 30000, Type = ProductType.Card },
            new() { Name = "Topup 50K", Description = "Recharge your account with 50,000 VND.", Price = 50000, Type = ProductType.Card },
            new() { Name = "Topup 100K", Description = "Recharge your account with 100,000 VND.", Price = 100000, Type = ProductType.Card, IsTop = true },
            new() { Name = "Topup 150K", Description = "Recharge your account with 150,000 VND.", Price = 150000, Type = ProductType.Card },
            new() { Name = "Topup 200K", Description = "Recharge your account with 200,000 VND.", Price = 200000, Type = ProductType.Card, IsSpecial = true },
            new() { Name = "Topup 300K", Description = "Recharge your account with 300,000 VND.", Price = 300000, Type = ProductType.Card },
            new() { Name = "Topup 500K", Description = "Recharge your account with 500,000 VND.", Price = 500000, Type = ProductType.Card, IsTop = true },
            new() { Name = "Topup 1M", Description = "Recharge your account with 1,000,000 VND.", Price = 1000000, Type = ProductType.Card, IsSpecial = true },
            new() { Name = "Promo Topup 50K + Bonus", Description = "A promotional recharge option with extra account value.", Price = 50000, Type = ProductType.Card },
            new() { Name = "Promo Topup 100K + Bonus", Description = "A seasonal recharge package that includes a small bonus.", Price = 100000, Type = ProductType.Card },
            new() { Name = "Monthly Recharge 200K", Description = "A convenient recharge amount for regular monthly mobile use.", Price = 200000, Type = ProductType.Card },
            new() { Name = "Family Recharge 300K", Description = "A stronger recharge choice for family or multi-service use.", Price = 300000, Type = ProductType.Card },
            new() { Name = "Business Recharge 500K", Description = "A high-value recharge option for business numbers and work devices.", Price = 500000, Type = ProductType.Card },
            new() { Name = "Quick Topup 15K", Description = "A small recharge for short urgent usage periods.", Price = 15000, Type = ProductType.Card },
            new() { Name = "Quick Topup 25K", Description = "A convenient low-cost recharge for extra call or SMS balance.", Price = 25000, Type = ProductType.Card },
            new() { Name = "Weekend Topup 75K", Description = "A recharge package sized for casual weekend communication and browsing.", Price = 75000, Type = ProductType.Card },
            new() { Name = "Flex Recharge 250K", Description = "A mid-tier recharge option for balanced data and call usage.", Price = 250000, Type = ProductType.Card },
            new() { Name = "Premium Recharge 750K", Description = "A premium-value recharge choice for heavy users.", Price = 750000, Type = ProductType.Card }
        };

        var callerTuneProducts = new List<Product>
        {
            new()
            {
                Name = "Caller Tune Monthly",
                Description = "Monthly access to caller tunes. Subscribe and pick one tune from the admin catalog.",
                Price = 20000,
                Type = ProductType.CallerTune,
                ValidDays = 30,
                IsTop = true
            }
        };

        return dataProducts
            .Concat(cardProducts)
            .Concat(callerTuneProducts)
            .ToList();
    }
}
