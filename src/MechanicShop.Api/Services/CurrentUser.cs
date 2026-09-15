using MechanicShop.Api.Extensions;
using MechanicShop.Application.Common.Interfaces;

namespace MechanicShop.Api.Services;

public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Id => _httpContextAccessor.HttpContext?.User?.GetUserId().ToString();
    public string? Name => _httpContextAccessor.HttpContext?.User?.GetUserName();
}