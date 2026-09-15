namespace MechanicShop.Api.Requests.Invoices;

public sealed record CreatePaymentLinkRequest(string SuccessUrl, string CancelUrl);