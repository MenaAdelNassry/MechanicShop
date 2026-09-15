public sealed record InvoicePdfDto
{
    public byte[] Content { get; init; } = [];
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/pdf";
}