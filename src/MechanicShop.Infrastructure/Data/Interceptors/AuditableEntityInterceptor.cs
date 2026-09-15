using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common;
using MechanicShop.Infrastructure.Caching;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Infrastructure.Data.Interceptors;

public class AuditableEntityInterceptor(
    IUser user,
    TimeProvider dateTime,
    ICacheInvalidationPublisher invalidationPublisher,
    ILogger<AuditableEntityInterceptor> logger) : SaveChangesInterceptor
{
    private readonly record struct ChangedEntity(string EntityName, string? EntityId);
    private HashSet<ChangedEntity>? _capturedChanges;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        CaptureChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        CaptureChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_capturedChanges != null && _capturedChanges.Count > 0)
        {
            logger.LogInformation("SavedChangesAsync triggered. Invalidation targets count: {Count}", _capturedChanges.Count);

            foreach (var change in _capturedChanges)
            {
                var entityPrefix = change.EntityName.ToLowerInvariant();

                if (!string.IsNullOrEmpty(change.EntityId))
                {
                    string specificEntityTag = $"{entityPrefix}_{change.EntityId}";
                    await invalidationPublisher.PublishInvalidationAsync(specificEntityTag, cancellationToken);
                }

                string listTag = $"{entityPrefix}_list";
                await invalidationPublisher.PublishInvalidationAsync(listTag, cancellationToken);
            }

            _capturedChanges = null;
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var utcNow = dateTime.GetUtcNow();
        var userId = user.Id;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(AuditableEntity.CreatedBy)).CurrentValue = userId;
                    entry.Property(nameof(AuditableEntity.CreatedAtUtc)).CurrentValue = utcNow;
                }

                entry.Property(nameof(AuditableEntity.LastModifiedBy)).CurrentValue = userId;
                entry.Property(nameof(AuditableEntity.LastModifiedUtc)).CurrentValue = utcNow;

                foreach (var ownedEntry in entry.References)
                {
                    if (ownedEntry.TargetEntry is { Entity: AuditableEntity } targetEntry &&
                        targetEntry.State is EntityState.Added or EntityState.Modified)
                    {
                        if (targetEntry.State == EntityState.Added)
                        {
                            targetEntry.Property(nameof(AuditableEntity.CreatedBy)).CurrentValue = userId;
                            targetEntry.Property(nameof(AuditableEntity.CreatedAtUtc)).CurrentValue = utcNow;
                        }

                        targetEntry.Property(nameof(AuditableEntity.LastModifiedBy)).CurrentValue = userId;
                        targetEntry.Property(nameof(AuditableEntity.LastModifiedUtc)).CurrentValue = utcNow;
                    }
                }
            }
        }
    }

    private void CaptureChanges(DbContext? context)
    {
        if (context == null) return;

        _capturedChanges = [];

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Extract the entity name and its primary key value (if available) for added, modified, or deleted entities
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var entityType = context.Model.FindEntityType(entry.Entity.GetType())?.ClrType ?? entry.Entity.GetType();
                var entityName = entityType.Name;

                // 1. Log the entity itself
                var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id" || p.Metadata.IsPrimaryKey());
                if (idProperty?.CurrentValue != null)
                {
                    _capturedChanges.Add(new ChangedEntity(entityName, idProperty.CurrentValue.ToString()));
                }

                // 2. Log the parent entities related via Foreign Keys
                foreach (var fk in entry.Metadata.GetForeignKeys())
                {
                    if (fk.PrincipalEntityType.ClrType is { } principalType)
                    {
                        var fkPropertyName = fk.Properties[0].Name;
                        var fkProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == fkPropertyName);

                        if (fkProperty?.CurrentValue != null)
                        {
                            _capturedChanges.Add(new ChangedEntity(principalType.Name, fkProperty.CurrentValue.ToString()));
                        }
                    }
                }
            }
        }
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry?.Metadata.IsOwned() == true &&
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}