namespace MechanicShop.Application.Features.AuditLogs.Dtos;

public sealed record AuditLogDto
{
    public Guid Id { get; init; }
    public string? UserId { get; init; }
    public string Type { get; init; } = default!;
    public string TableName { get; init; } = default!;
    public DateTimeOffset DateTimeUtc { get; init; }
    public string PrimaryKey { get; init; } = default!;
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? AffectedColumns { get; init; }
}