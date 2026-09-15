namespace MechanicShop.Application.Common.Interfaces;

public interface ISmsSender
{
    Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default);
}