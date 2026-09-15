using MediatR;

namespace MechanicShop.Domain.Common;

public abstract record DomainEvent : INotification
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}