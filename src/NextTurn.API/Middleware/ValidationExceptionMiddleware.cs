using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace NextTurn.API.Middleware;

/// <summary>
/// Catches FluentValidation.ValidationException thrown by the ValidationBehavior
/// pipeline and converts it into a structured HTTP 422 Unprocessable Entity
/// response using the RFC 7807 Problem Details format.
///
/// Why 422 instead of 400?
///   400 Bad Request means the server can't understand the request syntax.
///   422 Unprocessable Entity means the syntax is fine but business/validation
///   rules failed — semantically more accurate for failed validation.
///   This is consistent with RFC 9110 and common ASP.NET Core conventions.
///
/// Response shape (application/problem+json):
/// {
///   "type": "https://tools.ietf.org/html/rfc9110#section-15.5.21",
///   "title": "Validation Failed",
///   "status": 422,
///   "errors": {
///     "Email": ["Email format is invalid."],
///     "Password": ["Password must contain at least one uppercase letter."]
///   }
/// }
/// </summary>
public sealed class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationExceptionMiddleware> _logger;

    public ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation(
                "Validation failed for {Method} {Path}: {ErrorCount} error(s).",
                context.Request.Method,
                context.Request.Path,
                ex.Errors.Count());

            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            context.Response.ContentType = "application/problem+json";

            // Group failures by property name — each key maps to a list of messages.
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.21",
                Title = "Validation Failed",
                Status = StatusCodes.Status422UnprocessableEntity,
                Instance = context.Request.Path
            };

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
