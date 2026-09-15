using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Employees;

public static class EmployeeErrors
{
    public static readonly Error IdRequired =
        Error.Validation("Employee.IdRequired", "Employee Id is required.");

    public static readonly Error IdentityUserIdRequired =
        Error.Validation("Employee.IdentityUserIdRequired", "Identity user Id is required.");

    public static readonly Error RoleInvalid =
        Error.Validation("Employee.RoleInvalid", "Invalid role assigned to employee.");

    public static readonly Error NotFound =
        Error.NotFound("Employee.NotFound", "Employee was not found.");
}