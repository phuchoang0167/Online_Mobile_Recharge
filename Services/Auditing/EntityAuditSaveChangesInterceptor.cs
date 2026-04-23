using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Online_Mobile_Recharge.Models.Entities;

namespace Online_Mobile_Recharge.Services.Auditing;

public sealed class EntityAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConditionalWeakTable<DbContext, List<PendingAudit>> PendingAuditsByContext = new();
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EntityAuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private sealed record PendingAudit(
        EntityEntry Entry,
        string Action,
        string EntityType,
        object? OldData,
        object? NewData);

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context == null)
        {
            return base.SavingChanges(eventData, result);
        }

        var pending = BuildPendingAudits(eventData.Context);
        PendingAuditsByContext.Remove(eventData.Context);
        PendingAuditsByContext.Add(eventData.Context, pending);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        if (PendingAuditsByContext.TryGetValue(eventData.Context, out var pending) &&
            pending.Count > 0)
        {
            await PersistAuditLogsAsync(eventData.Context, pending, cancellationToken);
        }

        PendingAuditsByContext.Remove(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context != null)
        {
            PendingAuditsByContext.Remove(eventData.Context);
        }

        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            PendingAuditsByContext.Remove(eventData.Context);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private List<PendingAudit> BuildPendingAudits(DbContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return [];
        }

        var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ??
                   httpContext.Session.GetString("Role");

        if (!string.Equals(role, "Admin", StringComparison.Ordinal))
        {
            return [];
        }

        var adminUserId =
            int.TryParse(httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var claimUserId)
                ? claimUserId
                : (httpContext.Session.GetInt32("UserId") ?? 0);

        if (adminUserId <= 0)
        {
            return [];
        }

        var audits = new List<PendingAudit>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            // Prevent recursion: never audit audit-log writes.
            if (entry.Entity is AdminAuditLog)
            {
                continue;
            }

            // Skip entities without keys (shouldn't happen for normal tables).
            if (entry.Metadata.FindPrimaryKey() == null)
            {
                continue;
            }

            var entityType = entry.Metadata.ClrType.Name;
            var (action, oldData, newData) = entry.State switch
            {
                EntityState.Added => ("Create", null, Snapshot(entry, useOriginalValues: false)),
                EntityState.Modified => ("Update", Snapshot(entry, useOriginalValues: true), Snapshot(entry, useOriginalValues: false)),
                EntityState.Deleted => ("Delete", Snapshot(entry, useOriginalValues: true), null),
                _ => (string.Empty, null, null)
            };

            if (string.IsNullOrWhiteSpace(action))
            {
                continue;
            }

            // Stash admin id into the NewData/OldData payload so the audit record can show who did it,
            // but the actual AdminUserId is set at persistence time.
            if (newData is Dictionary<string, object?> newDict)
            {
                newDict["__adminUserId"] = adminUserId;
            }

            if (oldData is Dictionary<string, object?> oldDict)
            {
                oldDict["__adminUserId"] = adminUserId;
            }

            audits.Add(new PendingAudit(entry, action, entityType, oldData, newData));
        }

        return audits;
    }

    private async Task PersistAuditLogsAsync(DbContext sourceContext, List<PendingAudit> pending, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ??
                   httpContext.Session.GetString("Role");

        if (!string.Equals(role, "Admin", StringComparison.Ordinal))
        {
            return;
        }

        var adminUserId =
            int.TryParse(httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var claimUserId)
                ? claimUserId
                : (httpContext.Session.GetInt32("UserId") ?? 0);

        if (adminUserId <= 0)
        {
            return;
        }

        // Use a fresh DbContext instance to persist audit logs, so we don't interfere with the current unit-of-work.
        var scopeFactory = sourceContext.GetService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var auditDb = scope.ServiceProvider.GetRequiredService<MobileRechargeDbContext>();

        foreach (var item in pending)
        {
            var entityId = GetPrimaryKeyAsInt(item.Entry);

            var summary = $"Auto audit: {item.Action} {item.EntityType}#{entityId}.";
            if (summary.Length > 400)
            {
                summary = summary[..400];
            }

            auditDb.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminUserId = adminUserId,
                EntityType = item.EntityType,
                EntityId = entityId,
                Action = item.Action,
                Summary = summary,
                OldDataJson = item.OldData == null ? null : JsonSerializer.Serialize(item.OldData, JsonOptions),
                NewDataJson = item.NewData == null ? null : JsonSerializer.Serialize(item.NewData, JsonOptions),
                CreatedAt = DateTime.Now
            });
        }

        await auditDb.SaveChangesAsync(cancellationToken);
    }

    private static int GetPrimaryKeyAsInt(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey();
        if (pk == null)
        {
            return 0;
        }

        // Convention in this app: primary keys are int `Id`. Fallback to 0.
        var pkProp = pk.Properties.Count == 1 ? pk.Properties[0] : null;
        if (pkProp == null)
        {
            return 0;
        }

        var value = entry.Property(pkProp.Name).CurrentValue;
        if (value is int id)
        {
            return id;
        }

        return int.TryParse(value?.ToString(), out var parsed) ? parsed : 0;
    }

    private static object Snapshot(EntityEntry entry, bool useOriginalValues)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsShadowProperty())
            {
                continue;
            }

            var name = prop.Metadata.Name;
            object? value;

            if (useOriginalValues)
            {
                value = prop.OriginalValue;
            }
            else
            {
                value = prop.CurrentValue;
            }

            dict[name] = value;
        }

        return dict;
    }
}
