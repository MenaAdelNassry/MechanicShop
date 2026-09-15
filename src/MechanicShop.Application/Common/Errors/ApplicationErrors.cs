using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace MechanicShop.Application.Common.Errors;

public static class ApplicationErrors
{
    public static class Customers
    {
        public static readonly Error NotFound =
            Error.NotFound("Customer.NotFound", "Customer does not exist.");

        public static readonly Error VehicleInUse =
            Error.Conflict("Customer.VehicleInUse", "One or more vehicles are in use by active work orders.");

        public static readonly Error CustomerExists =
            Error.Conflict("Customer.CustomerExists", "A customer with this email already exists.");

        public static readonly Error CannotDeleteCustomerWithWorkOrders =
            Error.Conflict("Customer.CannotDeleteCustomerWithWorkOrders", "Cannot delete customer with associated work orders.");
    }

    public static class Vehicles
    {
        public static readonly Error NotFound =
            Error.NotFound("Vehicle.NotFound", "Vehicle does not exist.");

        public static readonly Error SchedulingConflict =
            Error.Conflict("Vehicle.SchedulingConflict", "The vehicle already has an overlapping WorkOrder.");
    }

    public static class WorkOrders
    {
        public static readonly Error NotFound =
            Error.NotFound("WorkOrder.NotFound", "WorkOrder does not exist.");

        public static Error OutsideOperatingHours(
            DateTimeOffset startAt,
            TimeZoneInfo workshopTimeZone,
            TimeOnly openingTime,
            TimeOnly closingTime)
        {
            var startLocalTime = TimeZoneInfo.ConvertTime(startAt, workshopTimeZone);

            string v = $"Requested time: {startLocalTime:HH:mm}.";
            return Error.Validation(
                "WorkOrder.OutsideOperatingHours",
                $"WorkOrder must be scheduled between {openingTime:hh\\:mm tt} and {closingTime:hh\\:mm tt}. " + v);
        }

        public static readonly Error LaborNotFound =
            Error.NotFound("WorkOrder.LaborNotFound", "The specified labor entry does not exist.");

        public static readonly Error LaborOccupied =
            Error.Conflict("WorkOrder.LaborOccupied", "The specified labor entry is already occupied by another WorkOrder.");

        public static readonly Error SpotIsNotAvailable =
            Error.Conflict("WorkOrder.SpotIsNotAvailable", "The specified spot is not available for the requested time.");

        public static readonly Error VehicleSchedulingConflict =
            Error.Conflict("WorkOrder.VehicleSchedulingConflict", "The vehicle already has an overlapping WorkOrder.");

        public static Error SpotNotAvailable(Spot spot, DateTimeOffset startAt, DateTimeOffset endAt)
        {
            return Error.Conflict("WorkOrder.SpotNotAvailable", $"The spot {spot} is not available from {startAt} to {endAt}.");
        }
    }

    public static class Auth
    {
        public static readonly Error RefreshTokenExpired =
            Error.Conflict("Auth.RefreshTokenExpired", "Refresh token is invalid or has expired.");

        public static readonly Error ExpiredAccessTokenInvalid =
            Error.Conflict("Auth.ExpiredAccessTokenInvalid", "The access token has expired and is invalid.");

        public static readonly Error UserIdClaimInvalid =
            Error.Conflict("Auth.UserIdClaimInvalid", "The userId claim is invalid.");
    }

    public static class Invoices
    {
        public static readonly Error NotFound =
            Error.NotFound("Invoice.NotFound", "Invoice does not exist.");

        public static readonly Error WorkOrderMustBeCompletedForInvoicing =
            Error.Conflict("WorkOrder.MustBeCompletedForInvoicing", "WorkOrder must be completed before invoicing.");

        public static readonly Error InvoiceAlreadyIssuedForWorkOrder =
            Error.Conflict("Invoice.AlreadyIssuedForWorkOrder", "An invoice has already been issued for this WorkOrder.");

        public static readonly Error AlreadyPaid =
            Error.Conflict("Invoice.AlreadyPaid", "The invoice has already been paid.");
    }

    public static class Inventory
    {
        public static readonly Error NotFound =
            Error.NotFound("InventoryItem.NotFound", "Inventory item does not exist.");

        public static readonly Error DuplicateName =
            Error.Conflict("InventoryItem.DuplicateName", "An inventory item with this name already exists.");
    }

    public static class RepairTasks
    {
        public static readonly Error NotFound =
            Error.NotFound("RepairTask.NotFound", "Repair task does not exist.");

        public static readonly Error DuplicateName =
            Error.Conflict("RepairTask.DuplicateName", "A repair task with this name already exists.");
    }

    public static class Employees
    {
        public static readonly Error NotFound =
            Error.NotFound("Employee.NotFound", "Employee does not exist.");
        public static readonly Error DuplicateEmail =
            Error.Conflict("Employee.DuplicateEmail", "An employee with this email already exists.");
    }
}