using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Customers.Vehicles;

public static class VehicleErrors
{
    public static readonly Error MakeRequired =
        Error.Validation("Vehicle.MakeRequired", "Vehicle make is required.");

    public static readonly Error ModelRequired =
        Error.Validation("Vehicle.ModelRequired", "Vehicle model is required.");

    public static readonly Error LicensePlateRequired =
        Error.Validation("Vehicle.LicensePlateRequired", "Vehicle license plate is required.");

    public static readonly Error YearInvalid =
        Error.Validation("Vehicle.YearInvalid", "Year must be between 1886 and next year.");
}