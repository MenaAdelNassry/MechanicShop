using Asp.Versioning;

using MechanicShop.Api.Requests.Invoices;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.Billing.Commands.CreateOnlinePaymentLink;
using MechanicShop.Application.Features.Billing.Commands.IssueInvoice;
using MechanicShop.Application.Features.Billing.Commands.ProcessStripeWebhook;
using MechanicShop.Application.Features.Billing.Commands.RecordPayment;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Application.Features.Billing.Queries.GetInvoiceById;
using MechanicShop.Application.Features.Billing.Queries.GetInvoicePdf;
using MechanicShop.Application.Features.Billing.Queries.GetInvoices;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Billing.Enums;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/invoices")]
[ApiVersion("1.0")]
[Authorize(Policy = "ManagerOnly")]
public sealed class InvoicesController(ISender sender) : ApiController
{
    [HttpPost("workorders/{workOrderId:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Issues an invoice for a work order.")]
    [EndpointDescription("Creates a new invoice for the specified work order and returns the created invoice resource.")]
    [EndpointName("IssueInvoiceForWorkOrder")]
    public async Task<IActionResult> IssueInvoice(
        Guid workOrderId,
        [FromBody] IssueInvoiceRequest request,
        CancellationToken ct)
    {
        var command = new IssueInvoiceCommand(workOrderId, request.DiscountAmount);
        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtRoute(nameof(GetInvoice), new { invoiceId = response.InvoiceId }, response),
            Problem);
    }

    [HttpGet]
    [EndpointSummary("Retrieves a paginated list of invoices.")]
    [EndpointDescription("Returns a paginated list of invoices based on the provided filters. Only users with the Manager role are authorized.")]
    [EndpointName("GetInvoices")]
    public async Task<ActionResult<PaginatedList<InvoiceDto>>> GetInvoices(
        [FromQuery] PageRequest pageRequest,
        [FromQuery] DateTimeOffset? fromDateUtc = null,
        [FromQuery] DateTimeOffset? toDateUtc = null,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = new GetInvoicesQuery(pageRequest.Page, pageRequest.PageSize, fromDateUtc, toDateUtc, status, searchTerm);
        var result = await sender.Send(query, ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{invoiceId:guid}", Name = nameof(GetInvoice))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves an invoice by ID.")]
    [EndpointDescription("Returns detailed information about the specified invoice. Only users with the Manager role are authorized.")]
    [EndpointName("GetInvoice")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(Guid invoiceId, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(invoiceId), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{invoiceId:guid}/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Downloads the invoice as a PDF file.")]
    [EndpointDescription("Returns the invoice PDF file for the specified invoice ID. Only users with the Manager role are authorized.")]
    [EndpointName("GetInvoicePdf")]
    public async Task<IActionResult> GetInvoicePdf(Guid invoiceId, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoicePdfQuery(invoiceId), ct);

        return result.Match(
          response => File(response.Content!, response.ContentType, response.FileName),
          Problem);
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize(Policy = "PaymentProcessingAccess")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Records a manual payment against an invoice.")]
    [EndpointDescription("Accepts full or partial payment (Cash, POS Card, Bank Transfer), assigns the current user as cashier, updates remaining balance, and transitions invoice status.")]
    [EndpointName("RecordInvoicePayment")]
    public async Task<ActionResult<PaymentDto>> RecordPayment(
    [FromRoute] Guid id,
    [FromBody] RecordPaymentRequest request,
    CancellationToken ct = default)
    {
        if (!Enum.TryParse<PaymentMethod>(request.Method, ignoreCase: true, out var method))
        {
            return Problem(detail: "Invalid payment method.", statusCode: StatusCodes.Status400BadRequest);
        }

        var command = new RecordPaymentCommand(id, request.Amount, method, request.TransactionReference);
        var result = await sender.Send(command, ct);

        return result.Match(Ok, Problem);
    }

    [HttpPost("{InvoiceId:guid}/create-payment-link")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Creates an online checkout session link for remaining invoice balance.")]
    [EndpointName("CreateInvoicePaymentLink")]
    public async Task<ActionResult<CreatePaymentLinkDto>> CreatePaymentLink(
    [FromRoute] Guid InvoiceId,
    [FromBody] CreatePaymentLinkRequest request,
    CancellationToken ct = default)
    {
        var command = new CreateOnlinePaymentLinkCommand(InvoiceId, request.SuccessUrl, request.CancelUrl);
        var result = await sender.Send(command, ct);

        return result.Match(Ok, Problem);
    }

    [HttpPost("stripe-webhook")]
    [AllowAnonymous]
    [EndpointSummary("Receives and processes Stripe webhook events.")]
    [EndpointDescription("Validates the cryptographic signature from Stripe and updates invoice payment status.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken ct)
    {
        // 1. Reading the payload as raw text to match the signature
        using var reader = new StreamReader(Request.Body);
        var jsonPayload = await reader.ReadToEndAsync(ct);

        // 2. Extracting the digital signature from the Headers
        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader))
        {
            return BadRequest("Missing Stripe-Signature header.");
        }

        // 3. Sending the Command to process the event and update the database
        var command = new ProcessStripeWebhookCommand(jsonPayload, signatureHeader!);
        var result = await sender.Send(command, ct);

        // 4. Stripe requires a 200 OK status code to acknowledge successful receipt of the event
        return result.Match(
            _ => Ok(),
            Problem);
    }
}