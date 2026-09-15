using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Identity;

namespace MechanicShop.Domain.Employees;

public sealed class Employee : AuditableEntity
{
    public Guid IdentityUserId { get; private set; }
    public PersonName Name { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; }
    public Role Role { get; private set; }

#pragma warning disable CS8618
    private Employee() { }
#pragma warning restore CS8618

    private Employee(
        Guid id,
        PersonName personName,
        PhoneNumber phoneNumber,
        Role role,
        Guid identityUserId)
        : base(id)
    {
        Name = personName;
        PhoneNumber = phoneNumber;
        Role = role;
        IdentityUserId = identityUserId;
    }

    public static Result<Employee> Create(
        Guid id,
        PersonName personName,
        PhoneNumber phoneNumber,
        Role role,
        Guid identityUserId)
    {
        if (id == Guid.Empty) return EmployeeErrors.IdRequired;
        if (!Enum.IsDefined(role)) return EmployeeErrors.RoleInvalid;
        if (identityUserId == Guid.Empty) return EmployeeErrors.IdentityUserIdRequired;

        return new Employee(id, personName, phoneNumber, role, identityUserId);
    }

    public Result<Updated> Update(PersonName newName, PhoneNumber newPhoneNumber, Role newRole)
    {
        if (!Enum.IsDefined(newRole))
        {
            return EmployeeErrors.RoleInvalid;
        }

        Name = newName;
        PhoneNumber = newPhoneNumber;
        Role = newRole;

        return Result.Updated;
    }
}