using System.ComponentModel.DataAnnotations;

namespace MechanicShop.Api.Requests.Customers;

public class CreateVehicleRequest
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
}