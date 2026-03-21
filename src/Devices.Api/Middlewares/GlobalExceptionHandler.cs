using System.Diagnostics.CodeAnalysis;
using Devices.Application.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Devices.Api.Middlewares;

[ExcludeFromCodeCoverage]
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
            RequestValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Business rule conflict"),
            DbUpdateException => (StatusCodes.Status409Conflict, "Database write conflict"),
            TimeoutException => (StatusCodes.Status503ServiceUnavailable, "Dependency timeout"),
            TaskCanceledException when !httpContext.RequestAborted.IsCancellationRequested
                => (StatusCodes.Status504GatewayTimeout, "Request timed out"),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested
                => (499, "Request canceled by client"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for request {Path}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception for request {Path}", httpContext.Request.Path);
        }

        var problemDetails = BuildProblemDetails(httpContext, exception, statusCode, title);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static ProblemDetails BuildProblemDetails(HttpContext httpContext, Exception exception, int statusCode, string title)
    {
        ProblemDetails details = exception switch
        {
            ValidationException validationException => BuildValidationProblemDetails(httpContext, validationException),
            _ => new ProblemDetails()
        };

        details.Status = statusCode;
        details.Title = title;
        details.Detail ??= exception.Message;
        details.Instance = httpContext.Request.Path;
        details.Type = $"https://httpstatuses.com/{statusCode}";

        details.Extensions["traceId"] = httpContext.TraceIdentifier;
        details.Extensions["errorCode"] = title.Replace(" ", string.Empty, StringComparison.Ordinal);
        details.Extensions["timestampUtc"] = DateTimeOffset.UtcNow;

        return details;
    }

    private static ValidationProblemDetails BuildValidationProblemDetails(HttpContext httpContext, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "request" : error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Detail = "One or more validation errors occurred.",
            Instance = httpContext.Request.Path
        };
    }
}
