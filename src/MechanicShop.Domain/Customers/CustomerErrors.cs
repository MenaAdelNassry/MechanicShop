using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Customers;

public static class CustomerErrors
{
    public static readonly Error CustomerNotFound =
        Error.NotFound("Customer.NotFound", "Customer was not found.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict("Customer.EmailAlreadyExists", "A customer with this email already exists.");

    public static readonly Error IdRequired =
        Error.Validation("Customer.IdRequired", "Customer ID is required.");

    public static readonly Error CannotDeleteWithActiveWorkOrders =
        Error.Conflict("Customer.CannotDeleteWithActiveWorkOrders", "Customer cannot be deleted due to active work orders.");
}