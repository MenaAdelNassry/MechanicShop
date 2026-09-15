using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Api.Infrastructure;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment env)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Handle Validation Failures (HTTP 400 Bad Request)
        if (exception is FluentValidation.ValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            // Transform FluentValidation errors into a standard dictionary format (Property -> ErrorMessages)
            var validationErrors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more validation errors occurred.",
                    Detail = "Please correct the specified errors and try again.",
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errors"] = validationErrors } // Inject structural errors for frontend parsing
                }
            });
        }

        // 2. Handle Database Concurrency Conflicts (HTTP 409 Conflict)
        if (exception is DbUpdateConcurrencyException concurrencyException)
        {
            logger.LogWarning(concurrencyException, "A concurrency conflict occurred while updating data.");

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Title = "Data Concurrency Conflict",
                    Status = StatusCodes.Status409Conflict,
                    Detail = "The record you attempted to edit was modified by another user after you got the original values. Please reload and try again."
                }
            });
        }

        // 3. Handle Server Failures (HTTP 500 Internal Server Error)
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var detail = env.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred on our server. Please try again later.";

        var type = env.IsDevelopment()
            ? exception.GetType().Name
            : "https://tools.ietf.org/html/rfc7231#section-6.6.1";

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = type,
                Title = "An error occurred while processing your request.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = detail,
            }
        });
    }
}