using MechanicShop.Application.Common.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Infrastructure.Identity.Policies;

public class LaborAssignedRequirement : IAuthorizationRequirement;

public class LaborAssignedHandler(IAppDbContext context, IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<LaborAssignedRequirement>
{
    private readonly IAppDbContext _context = context;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, LaborAssignedRequirement requirement)
    {
        if (context.User.IsInRole("Manager"))
        {
            context.Succeed(requirement);
            return;
        }

        var employeeIdClaim = context.User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return;
        }

        var workOrderIdString = _httpContextAccessor.HttpContext?.Request.RouteValues["WorkOrderId"]?.ToString();

        if (!Guid.TryParse(workOrderIdString, out var workOrderId))
        {
            return;
        }

        var isAssigned = await _context.WorkOrders
            .AnyAsync(wo => wo.Id == workOrderId && wo.LaborId == employeeId);

        if (isAssigned)
        {
            context.Succeed(requirement);
        }
    }
}