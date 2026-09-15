using Asp.Versioning;

using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Application.Features.Billing.Queries.GetDailyCashDrawerSummary;
using MechanicShop.Application.Features.Reports.Dtos;
using MechanicShop.Application.Features.Reports.Queries.GetPartsUsageReport;
using MechanicShop.Application.Features.Reports.Queries.GetRevenueSummary;
using MechanicShop.Application.Features.Reports.Queries.GetTechnicianProductivity;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/reports/revenue")]
[ApiVersion("1.0")]
[Authorize]
public sealed class RevenueReportsController(ISender sender) : ApiController
{
    [HttpGet("summary")]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Gets the revenue summary and daily breakdown for a date range.")]
    [EndpointDescription("Calculates net revenue, gross subtotal, discounts, taxes, and daily chart data based on paid invoices within the specified local timezone.")]
    [EndpointName("GetRevenueSummary")]
    public async Task<ActionResult<RevenueSummaryDto>> GetRevenueSummary(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] string timeZoneId = "UTC",
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetRevenueSummaryQuery(fromDate, toDate, timeZoneId), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("technicians")]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Gets technician productivity and efficiency metrics.")]
    [EndpointDescription("Calculates estimated vs actual working hours, task completion counts, efficiency ratios, and generated labor revenue per technician.")]
    [EndpointName("GetTechnicianProductivity")]
    public async Task<ActionResult<TechniciansReportDto>> GetTechnicianProductivity(
    [FromQuery] DateOnly fromDate,
    [FromQuery] DateOnly toDate,
    [FromQuery] Guid? laborId = null,
    [FromQuery] string timeZoneId = "UTC",
    CancellationToken ct = default)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
        }

        var result = await sender.Send(new GetTechnicianProductivityQuery(fromDate, toDate, timeZone, laborId), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("parts-usage")]
    [Authorize(Policy = "InventoryManagementAccess")]
    [EndpointSummary("Gets parts consumption and inventory usage analytics.")]
    [EndpointDescription("Aggregates total quantities, financial cost, work order frequency, and current stock alert levels for consumed parts within the specified period.")]
    [EndpointName("GetPartsUsageReport")]
    public async Task<ActionResult<PartsUsageReportDto>> GetPartsUsageReport(
    [FromQuery] DateOnly fromDate,
    [FromQuery] DateOnly toDate,
    [FromQuery] string timeZoneId = "UTC",
    CancellationToken ct = default)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
        }

        var result = await sender.Send(new GetPartsUsageReportQuery(fromDate, toDate, timeZone), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("cash-drawer")]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Gets daily cash drawer totals and method-wise reconciliation.")]
    [EndpointDescription("Summarizes all finalized transactions for the given calendar day grouped by Cash, POS, Bank Transfer, and Online channels.")]
    [EndpointName("GetDailyCashDrawerSummary")]
    public async Task<ActionResult<CashDrawerSummaryDto>> GetDailyCashDrawer(
    [FromQuery] DateOnly date,
    [FromQuery] string timeZoneId = "UTC",
    CancellationToken ct = default)
    {
        var result = await sender.Send(new GetDailyCashDrawerSummaryQuery(date, timeZoneId), ct);

        return result.Match(Ok, Problem);
    }
}