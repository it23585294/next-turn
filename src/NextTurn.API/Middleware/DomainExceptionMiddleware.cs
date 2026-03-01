using NextTurn.Domain.Common;
using System.Text.Json;

namespace NextTurn.API.Middleware;

/// <summary>
/// Catches NextTurn.Domain.Common.DomainException thrown by domain entities or
/// the application handler and converts it into a structured HTTP 400 Bad Request
/// response using the RFC 7807 Problem Details format.
///
/// DomainException represents a violated business rule — it is always the caller's
/// fault (e.g. email already in use, invalid entity state), so 400 is correct.
///
/// Response shape (application/problem+json):
/// {
///   "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
///   "title": "Business Rule Violation",
///   "status": 400,
///   "detail": "Email address is already in use."
/// }
/// </summary>
public sealed class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMiddleware> _logger;

    public DomainExceptionMiddleware(RequestDelegate next, ILogger<DomainExceptionMiddleware> logger)
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
        catch (DomainException ex)
        {
            // ConflictDomainException (subclass) → 409 Conflict
            // Base DomainException → 400 Bad Request
            bool isConflict = ex is ConflictDomainException;

            int statusCode = isConflict
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

            string title = isConflict ? "Conflict" : "Business Rule Violation";

            _logger.LogInformation(
                "Domain rule violated at {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title,
                status = statusCode,
                detail = ex.Message,
                instance = context.Request.Path.ToString()
            };

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
