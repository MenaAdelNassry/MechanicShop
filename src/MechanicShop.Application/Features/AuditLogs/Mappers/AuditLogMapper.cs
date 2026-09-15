using MechanicShop.Application.Features.AuditLogs.Dtos;
using MechanicShop.Domain.Common;

namespace MechanicShop.Application.Features.Billing.Mappers;

public static class AuditLogMapper
{
    public static AuditLogDto ToDto(this AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            Type = auditLog.Type,
            TableName = auditLog.TableName,
            DateTimeUtc = auditLog.DateTimeUtc,
            PrimaryKey = auditLog.PrimaryKey,
            AffectedColumns = auditLog.AffectedColumns,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues
        };
    }
}