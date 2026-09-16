using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Spots;

public sealed class ServiceBay : Entity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private ServiceBay() { } // Required for EF Core

    private ServiceBay(Guid id, string name, string? description, bool isActive)
        : base(id)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
    }

    public static ServiceBay Create(string name, string? description = null)
    {
        return new ServiceBay(
            id: Guid.NewGuid(),
            name: name.Trim(),
            description: description?.Trim(),
            isActive: true);
    }

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void ToggleActive()
    {
        IsActive = !IsActive;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}