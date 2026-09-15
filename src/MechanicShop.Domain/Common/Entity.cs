namespace MechanicShop.Domain.Common;

public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; }

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
    }

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        if (!_domainEvents.Contains(domainEvent))
        {
            _domainEvents.Add(domainEvent);
        }
    }

    public void RemoveDomainEvent(DomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool Equals(Entity? other)
    {
        if (other is null || other.GetType() != GetType()) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id != Guid.Empty && Id == other.Id;
    }

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);
    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}