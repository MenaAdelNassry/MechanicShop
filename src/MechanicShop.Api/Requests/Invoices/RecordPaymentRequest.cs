namespace MechanicShop.Api.Requests.Invoices;

public sealed record RecordPaymentRequest(
    decimal Amount,
    string Method,
    string? TransactionReference
);