using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Billing.Queries.GetDailyCashDrawerSummary;

public sealed class GetDailyCashDrawerSummaryQueryHandler(IAppDbContext context)
    : IRequestHandler<GetDailyCashDrawerSummaryQuery, Result<CashDrawerSummaryDto>>
{
    public async Task<Result<CashDrawerSummaryDto>> Handle(GetDailyCashDrawerSummaryQuery query, CancellationToken ct)
    {
        // 1. Specify the required time zone
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(query.TimeZoneId ?? "Africa/Cairo");

        // 2. Convert the start and end of the specified day in local time to UTC
        var localStart = query.Date.ToDateTime(TimeOnly.MinValue);
        var localEnd = query.Date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);

        // 3. Retrieve the completed payments for the specified day
        var completedPayments = await context.Payments
            .Where(p =>
                p.Status == PaymentStatus.Completed &&
                p.PaidAtUtc >= utcStart &&
                p.PaidAtUtc < utcEnd)
            .AsNoTracking()
            .ToListAsync(ct);

        // 4. Build the details for each payment method once
        var breakdown = completedPayments
            .GroupBy(p => p.Method)
            .Select(g => new PaymentMethodBreakdownDto
            {
                Method = g.Key,
                TransactionsCount = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(b => b.TotalAmount)
            .ToList();

        // 5. Extract the totals directly from the breakdown
        var cashTotal = breakdown.FirstOrDefault(b => b.Method == PaymentMethod.Cash)?.TotalAmount ?? 0m;
        var posTotal = breakdown.FirstOrDefault(b => b.Method == PaymentMethod.PosCard)?.TotalAmount ?? 0m;
        var bankTransferTotal = breakdown.FirstOrDefault(b => b.Method == PaymentMethod.BankTransfer)?.TotalAmount ?? 0m;
        var onlineGatewayTotal = breakdown.FirstOrDefault(b => b.Method == PaymentMethod.OnlineGateway)?.TotalAmount ?? 0m;
        var grandTotal = breakdown.Sum(b => b.TotalAmount);

        return new CashDrawerSummaryDto
        {
            Date = query.Date,
            GrandTotalCollected = grandTotal,
            TotalTransactionsCount = completedPayments.Count,
            BreakdownByMethod = breakdown
        };
    }
}