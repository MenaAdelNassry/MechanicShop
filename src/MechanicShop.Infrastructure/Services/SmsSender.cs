using MechanicShop.Application.Common.Interfaces;

using Microsoft.Extensions.Logging;

namespace MechanicShop.Infrastructure.Services;

public sealed class SmsSender(ILogger<SmsSender> logger) : ISmsSender
{
    public Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        var masked = phoneNumber.Length >= 4
            ? new string('*', phoneNumber.Length - 4) + phoneNumber[^4..]
            : "****";

        logger.LogInformation("[SMS] To: {Phone} | Message: {Message}", masked, message);
        return Task.CompletedTask;
    }
}