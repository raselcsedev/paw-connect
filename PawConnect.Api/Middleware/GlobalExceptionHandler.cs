using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        ProblemDetails problem = exception switch
        {
            NotFoundException => Build(StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            ConflictException => Build(StatusCodes.Status409Conflict, "Conflict", exception.Message),
            AuthenticationFailedException => Build(StatusCodes.Status401Unauthorized, "Authentication failed", exception.Message),
            ForbiddenException => Build(StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            AppValidationException validation => new ValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            },
            _ => Build(StatusCodes.Status500InternalServerError, "An unexpected error occurred", "Please try again later.")
        };

        problem.Instance = httpContext.Request.Path;

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);
        }

        if (problem.Status == StatusCodes.Status401Unauthorized)
        {
            httpContext.Response.Headers.WWWAuthenticate = "Bearer";
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }

    private static ProblemDetails Build(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };
}