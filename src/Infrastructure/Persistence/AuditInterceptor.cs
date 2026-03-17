using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Rentolic.Domain.Entities;
using System.Text.Json;

namespace Rentolic.Infrastructure.Persistence;

public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = new AuditLog
            {
                ResourceType = entry.Entity.GetType().Name,
                ResourceId = entry.Property("Id").CurrentValue?.ToString(),
                Action = entry.State.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            var changes = new Dictionary<string, object?>();
            foreach (var property in entry.Properties)
            {
                // Mask sensitive data
                if (property.Metadata.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)) continue;

                if (entry.State == EntityState.Modified && property.IsModified)
                {
                    changes[property.Metadata.Name] = new { Old = property.OriginalValue, New = property.CurrentValue };
                }
                else if (entry.State == EntityState.Added)
                {
                    changes[property.Metadata.Name] = property.CurrentValue;
                }
            }
            auditLog.Diff = JsonSerializer.Serialize(changes);

            // We need to add the log to the context, but be careful with recursion if using SaveChanges
            // In a real production app, we might use a separate context or a raw SQL command
            // For this unified module, we'll use context.Set<AuditLog>().Add(auditLog);
            if (entry.Entity is not AuditLog)
            {
                 context.Set<AuditLog>().Add(auditLog);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
