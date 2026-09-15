using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Notifications;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Identity;

using MediatR;

using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Employees.Commands.CreateEmployee;

public class CreateEmployeeCommandHandler(
    IAppDbContext context,
    IIdentityService identityService,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    ILogger<CreateEmployeeCommandHandler> logger
) : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken ct)
    {
        var nameResult = PersonName.Create(request.FirstName, request.LastName);
        if (nameResult.IsError) return nameResult.Errors;

        if (!Enum.TryParse<Role>(request.Role, ignoreCase: true, out var parsedRole))
            return EmployeeErrors.RoleInvalid;

        var phoneResult = PhoneNumber.Create(request.PhoneNumber);
        if (phoneResult.IsError) return phoneResult.Errors;

        var userId = Guid.CreateVersion7();
        var tempPassword = Guid.CreateVersion7().ToString() + "W!9";

        await using var transaction = await context.BeginTransactionAsync(ct);

        try
        {
            var createUserResult = await identityService.CreateUserAsync(
                id: userId,
                userName: request.Email,
                email: request.Email,
                password: tempPassword,
                role: parsedRole.ToString(),
                emailConfirmed: false,
                ct: ct);

            if (createUserResult.IsError)
                return createUserResult.Errors;

            var employeeId = Guid.CreateVersion7();
            var employeeResult = Employee.Create(
                employeeId,
                nameResult.Value,
                phoneResult.Value,
                parsedRole,
                userId);

            if (employeeResult.IsError)
                return employeeResult.Errors;

            context.Employees.Add(employeeResult.Value);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            var tokenResult = await identityService.GeneratePasswordResetTokenByEmailAsync(request.Email, ct);
            if (tokenResult.IsError)
                return tokenResult.Errors;

            var messageHtml = EmployeeWelcomeEmailBuilder.BuildHtml(request.Email, tokenResult.Value);
            var plainText = EmployeeWelcomeEmailBuilder.BuildPlainText(request.Email, tokenResult.Value);

            var fullEmailHtml = await templateRenderer.RenderAsync(
                title: EmployeeWelcomeEmailBuilder.Title,
                recipientName: nameResult.Value.FullName,
                messageHtml: messageHtml,
                ct: ct);

            await emailSender.SendEmailAsync(
                to: request.Email,
                subject: EmployeeWelcomeEmailBuilder.Subject,
                htmlBody: fullEmailHtml,
                textBody: plainText,
                ct: ct);

            logger.LogInformation("Employee {EmployeeId} created and invitation email sent.", employeeId);

            return employeeId;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Transaction rolled back. Failed to create employee for user {UserId}", userId);
            throw;
        }
    }
}