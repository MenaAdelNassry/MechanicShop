using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Identity.Commands.SetPassword;

public sealed class SetPasswordCommandHandler(IIdentityService identityService)
    : IRequestHandler<SetPasswordCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(SetPasswordCommand request, CancellationToken ct)
    {
        return await identityService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, ct);
    }
}