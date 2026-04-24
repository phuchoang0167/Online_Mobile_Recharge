using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Models.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string? BillingAddress { get; set; }
        public bool EmailVerified { get; set; }
        public string? EmailVerificationTokenHash { get; set; }
        public DateTime? EmailVerificationTokenExpiresAt { get; set; }
        public string? PasswordResetTokenHash { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? LastLockReason { get; set; }
        public string? LastLockNote { get; set; }
        public DateTime? LastLockedAt { get; set; }

        public int FailedLoginAttempts { get; set; }
        public DateTime? FailedLoginDate { get; set; }
        public DateTime? LoginLockoutUntil { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
