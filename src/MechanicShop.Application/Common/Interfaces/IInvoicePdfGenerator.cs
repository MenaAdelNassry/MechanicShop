using MechanicShop.Domain.Workorders.Billing;

namespace MechanicShop.Application.Common.Interfaces;

public interface IInvoicePdfGenerator
{
    byte[] Generate(Invoice invoice);
}