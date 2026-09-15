using Asp.Versioning;

using MechanicShop.Api.Requests.Invoices;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Features.Billing.Commands.CreateOnlinePaymentLink;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Application.Features.WorkOrders.Queries.GetPublicTrackingInfo;
using MechanicShop.Domain.Workorders.Billing;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tracking")]
[AllowAnonymous]
public sealed class PublicTrackingController(ISender sender) : ApiController
{
    [HttpGet("{trackingToken:guid}")]
    [EndpointSummary("Retrieve public live tracking information for a vehicle repair order.")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicTrackingDto>> GetTrackingInfo(
        [FromRoute] Guid trackingToken,
        CancellationToken ct)
    {
        var query = new GetPublicTrackingInfoQuery(trackingToken);
        var result = await sender.Send(query, ct);

        return result.Match(Ok, Problem);
    }

    [HttpPost("{trackingToken:guid}/pay")]
    [EndpointSummary("Create an online checkout link for the remaining invoice balance using the tracking token.")]
    [ProducesResponseType(typeof(CreatePaymentLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatePaymentLinkDto>> CreatePaymentLinkForCustomer(
        [FromRoute] Guid trackingToken,
        [FromBody] CreatePaymentLinkRequest request,
        CancellationToken ct)
    {
        var trackingQuery = new GetPublicTrackingInfoQuery(trackingToken);
        var trackingResult = await sender.Send(trackingQuery, ct);

        if (trackingResult.IsError)
        {
            return Problem(trackingResult.Errors);
        }

        var trackingInfo = trackingResult.Value;

        if (!trackingInfo.HasInvoice || !trackingInfo.InvoiceId.HasValue)
        {
            return Problem([ApplicationErrors.Invoices.NotFound]);
        }

        if (trackingInfo.RemainingBalance is null or <= 0 || trackingInfo.InvoiceStatus == InvoiceStatus.Paid)
        {
            return Problem([ApplicationErrors.Invoices.AlreadyPaid]);
        }

        var paymentCommand = new CreateOnlinePaymentLinkCommand(
            InvoiceId: trackingInfo.InvoiceId.Value,
            SuccessUrl: request.SuccessUrl,
            CancelUrl: request.CancelUrl);

        var paymentResult = await sender.Send(paymentCommand, ct);

        return paymentResult.Match(Ok, Problem);
    }
}