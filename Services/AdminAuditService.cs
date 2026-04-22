using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Services;

public class AdminAuditService
{
    private readonly MobileRechargeDbContext _context;

    public AdminAuditService(MobileRechargeDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync<T>(int adminUserId, string action, string summary, T? oldData, T? newData, int entityId, CancellationToken cancellationToken = default)
    {
        var log = new AdminAuditLog
        {
            AdminUserId = adminUserId,
            EntityType = typeof(T).Name,
            EntityId = entityId,
            Action = action,
            Summary = summary,
            OldDataJson = oldData == null ? null : JsonSerializer.Serialize(oldData),
            NewDataJson = newData == null ? null : JsonSerializer.Serialize(newData),
            CreatedAt = DateTime.Now
        };

        _context.AdminAuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<User>> GetUsersWhoPurchasedProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var userIds = await _context.Transactions
            .AsNoTracking()
            .Where(x => x.ProductId == productId && x.Status == Models.Enums.TransactionStatus.Success)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _context.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id) && x.Role == "User" && !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}

