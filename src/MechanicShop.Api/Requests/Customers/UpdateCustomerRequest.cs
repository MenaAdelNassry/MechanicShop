namespace MechanicShop.Api.Requests.Customers;

public class UpdateCustomerRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public List<UpdateVehicleRequest> Vehicles { get; set; } = [];
}