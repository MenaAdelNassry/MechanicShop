using System.Diagnostics.Metrics;

namespace MechanicShop.Application.Metrices;

public static class MechanicShopMetrics
{
    public static readonly Meter Meter = new("MechanicShop.Api", "1.0.0");

    public static readonly Counter<long> WorkOrdersCreated =
        Meter.CreateCounter<long>("mechanicshop_work_orders_created_total", "Count", "Total number of created work orders");

    public static readonly Counter<long> InvoicesIssued =
        Meter.CreateCounter<long>("mechanicshop_invoices_issued_total", "Count", "Total number of issued invoices");
}