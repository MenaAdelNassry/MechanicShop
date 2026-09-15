using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Reports.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Reports.Queries.GetRevenueSummary;

public sealed class GetRevenueSummaryQueryHandler(IAppDbContext context)
    : IRequestHandler<GetRevenueSummaryQuery, Result<RevenueSummaryDto>>
{
    public async Task<Result<RevenueSummaryDto>> Handle(GetRevenueSummaryQuery query, CancellationToken ct)
    {
        TimeZoneInfo parsedTimeZone = GetTimeZone(query.TimeZone);

        // 1. Convert the required time range from local time to UTC
        var localStart = query.FromDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = query.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, parsedTimeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, parsedTimeZone);

        // 2. Get all paid invoices within the UTC time range
        var paidInvoices = await context.Invoices
            .Where(inv =>
                inv.Status == InvoiceStatus.Paid &&
                inv.PaidAt.HasValue &&
                inv.PaidAt.Value >= utcStart &&
                inv.PaidAt.Value < utcEnd)
            .Include(inv => inv.LineItems)
            .AsNoTracking()
            .ToListAsync(ct);

        // 3. Fast grouping by local date using Lookup O(1)
        var invoicesByDate = paidInvoices
            .ToLookup(inv => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(inv.PaidAt!.Value, parsedTimeZone).DateTime));

        var dailyBreakdown = new List<RevenueDailyPointDto>();

        for (var date = query.FromDate; date <= query.ToDate; date = date.AddDays(1))
        {
            var dayInvoices = invoicesByDate[date].ToList();

            var daySubtotal = dayInvoices.Sum(inv => inv.Subtotal);
            var dayDiscounts = dayInvoices.Sum(inv => inv.DiscountAmount);
            var dayTaxes = dayInvoices.Sum(inv => inv.TaxAmount);
            var dayTotal = dayInvoices.Sum(inv => inv.Total);
            var dayCount = dayInvoices.Count;

            dailyBreakdown.Add(new RevenueDailyPointDto
            {
                Date = date,
                TotalRevenue = dayTotal,
                Subtotal = daySubtotal,
                Discounts = dayDiscounts,
                Taxes = dayTaxes,
                PaidInvoicesCount = dayCount
            });
        }

        // 4. Calculate total values and key performance indicators (KPIs)
        var totalSubtotal = paidInvoices.Sum(inv => inv.Subtotal);
        var totalDiscounts = paidInvoices.Sum(inv => inv.DiscountAmount);
        var totalTaxes = paidInvoices.Sum(inv => inv.TaxAmount);
        var totalNetRevenue = paidInvoices.Sum(inv => inv.Total);
        var totalCount = paidInvoices.Count;
        var avgValue = totalCount > 0 ? totalNetRevenue / totalCount : 0m;

        return new RevenueSummaryDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            TotalNetRevenue = totalNetRevenue,
            TotalSubtotal = totalSubtotal,
            TotalDiscounts = totalDiscounts,
            TotalTaxes = totalTaxes,
            TotalPaidInvoices = totalCount,
            AverageInvoiceValue = Math.Round(avgValue, 2),
            DailyBreakdown = dailyBreakdown
        };
    }

    private static TimeZoneInfo GetTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}