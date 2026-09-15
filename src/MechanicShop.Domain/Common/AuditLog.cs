namespace MechanicShop.Domain.Common;

public class AuditLog : Entity
{
    public string? UserId { get; set; }
    public string Type { get; set; } = default!; // Added, Modified, Deleted
    public string TableName { get; set; } = default!;
    public DateTimeOffset DateTimeUtc { get; set; }
    public string PrimaryKey { get; set; } = default!;
    public string? OldValues { get; set; } // JSON String
    public string? NewValues { get; set; } // JSON String
    public string? AffectedColumns { get; set; } // JSON Array String (Names of fields that were modified)
}