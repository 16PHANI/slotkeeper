using System.Net;
using System.Text.Json;
using SlotKeeper.Domain.Exceptions;

namespace SlotKeeper.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            var (statusCode, title) = MapException(ex);

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogWarning(ex, "Request {Method} {Path} failed with {StatusCode}", context.Request.Method, context.Request.Path, statusCode);
            }

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var problem = new
            {
                title,
                status = (int)statusCode,
                detail = ex.Message,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }

    private static (HttpStatusCode StatusCode, string Title) MapException(Exception ex) => ex switch
    {
        BookingConflictException => (HttpStatusCode.Conflict, "Booking conflict"),
        BookingLimitExceededException => (HttpStatusCode.Conflict, "Booking limit exceeded"),
        ConflictException => (HttpStatusCode.Conflict, "Conflict"),
        InvalidCredentialsException => (HttpStatusCode.Unauthorized, "Invalid credentials"),
        InvalidBookingWindowException => (HttpStatusCode.BadRequest, "Invalid booking window"),
        EntityNotFoundException => (HttpStatusCode.NotFound, "Not found"),
        UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Forbidden"),
        _ => (HttpStatusCode.InternalServerError, "Unexpected error")
    };
}
