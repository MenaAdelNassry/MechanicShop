using MechanicShop.Application.Common.Interfaces;

using MediatR.Pipeline;

using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest>(ILogger<LoggingBehaviour<TRequest>> logger, IUser user)
    : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest>> _logger = logger;
    private readonly IUser _user = user;

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _user.Id ?? string.Empty;
        var userName = _user.Name ?? string.Empty;

        _logger.LogInformation(
            "Request: {Name} {@UserId} {@UserName} {@Request}",
            requestName,
            userId,
            userName,
            request);

        return Task.CompletedTask;
    }
}