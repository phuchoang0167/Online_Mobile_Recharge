using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;

public class MobileRechargeDbContext : DbContext
{
    public MobileRechargeDbContext(DbContextOptions<MobileRechargeDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<DndSetting> DndSettings { get; set; }
    public DbSet<DndNumber> DndNumbers { get; set; }
    public DbSet<CallerTune> CallerTunes { get; set; }
    public DbSet<UserCallerTuneSetting> UserCallerTuneSettings { get; set; }
    public DbSet<CallerTuneSubscription> CallerTuneSubscriptions { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductSale> ProductSales { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<PhoneDataSubscription> PhoneDataSubscriptions { get; set; }
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ================= ENUM =================
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Transaction>()
            .Property(t => t.PaymentMethod)
            .HasConversion<string>();

        modelBuilder.Entity<Product>()
            .Property(p => p.Type)
            .HasConversion<string>();

        modelBuilder.Entity<ProductSale>()
            .Property(p => p.SaleType)
            .HasConversion<string>();

        modelBuilder.Entity<Sale>()
            .Property(p => p.SaleType)
            .HasConversion<string>();

        modelBuilder.Entity<ProductSale>()
            .HasIndex(x => new { x.ProductId, x.StartAt, x.EndAt });

        modelBuilder.Entity<ProductSale>()
            .HasOne(x => x.Product)
            .WithMany(x => x.Sales)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Sale>()
            .HasIndex(x => new { x.SaleType, x.StartAt, x.EndAt, x.IsEnabled });

        modelBuilder.Entity<PhoneDataSubscription>()
            .HasIndex(x => new { x.PhoneNumber, x.ProductId })
            .IsUnique();

        modelBuilder.Entity<PhoneDataSubscription>()
            .Property(x => x.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<PhoneDataSubscription>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DndSetting>()
            .Property(d => d.Mode)
            .HasConversion<string>();

        modelBuilder.Entity<UserCallerTuneSetting>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<UserCallerTuneSetting>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserCallerTuneSetting>()
            .HasOne(x => x.CallerTune)
            .WithMany()
            .HasForeignKey(x => x.CallerTuneId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CallerTuneSubscription>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<CallerTuneSubscription>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        // ================= DECIMAL FIX =================
        modelBuilder.Entity<Card>()
            .Property(c => c.Balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Card>()
            .Property(c => c.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ProductSale>()
            .Property(p => p.SaleValue)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Sale>()
            .Property(p => p.SaleValue)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.PostpaidNationalId)
            .HasMaxLength(30);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.PostpaidBillingAddress)
            .HasMaxLength(500);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.PaymentExternalId)
            .HasMaxLength(100);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.PaymentExternalPayerId)
            .HasMaxLength(100);


        // ================= BONUS =================
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();


        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.EmailVerificationTokenHash)
            .HasMaxLength(128);

        modelBuilder.Entity<User>()
            .Property(u => u.PasswordResetTokenHash)
            .HasMaxLength(128);

        modelBuilder.Entity<User>()
            .Property(u => u.Name)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.NationalId)
            .HasMaxLength(30);

        modelBuilder.Entity<User>()
            .Property(u => u.BillingAddress)
            .HasMaxLength(500);

        modelBuilder.Entity<User>()
            .Property(u => u.LastLockReason)
            .HasMaxLength(150);

        modelBuilder.Entity<User>()
            .Property(u => u.LastLockNote)
            .HasMaxLength(500);

        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.Email, u.FailedLoginDate });

        modelBuilder.Entity<Transaction>()
            .Property(t => t.PhoneNumber)
            .IsRequired();

        modelBuilder.Entity<AdminAuditLog>()
            .Property(x => x.EntityType)
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<AdminAuditLog>()
            .Property(x => x.Action)
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<AdminAuditLog>()
            .Property(x => x.Summary)
            .HasMaxLength(400)
            .IsRequired();

        modelBuilder.Entity<Feedback>()
            .Property(f => f.Category)
            .HasMaxLength(50)
            .HasDefaultValue("Support")
            .IsRequired();

        modelBuilder.Entity<Feedback>()
            .Property(f => f.Name)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<Feedback>()
            .Property(f => f.Email)
            .HasMaxLength(255)
            .IsRequired();

        modelBuilder.Entity<Feedback>()
            .Property(f => f.AdminReply)
            .HasMaxLength(1500);

        modelBuilder.Entity<Feedback>()
            .HasIndex(f => new { f.ProductId, f.CreatedAt });

        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.User)
            .WithMany(u => u.Feedbacks)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.Product)
            .WithMany(p => p.Feedbacks)
            .HasForeignKey(f => f.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.Transaction)
            .WithMany(t => t.Feedbacks)
            .HasForeignKey(f => f.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
