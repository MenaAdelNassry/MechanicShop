namespace MechanicShop.Domain.Common;

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity() { }

    protected AuditableEntity(Guid id)
        : base(id) { }

    public DateTimeOffset CreatedAtUtc { get; protected internal set; }
    public string? CreatedBy { get; protected internal set; }
    public DateTimeOffset LastModifiedUtc { get; protected internal set; }
    public string? LastModifiedBy { get; protected internal set; }
}