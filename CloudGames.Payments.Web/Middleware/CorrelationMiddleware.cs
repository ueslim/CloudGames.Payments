using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace CloudGames.Payments.Web.Middleware;

public class CorrelationMiddleware
{
    private const string CorrelationHeader = "x-correlation-id";
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationHeader, out var existing)
            ? existing.ToString()
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        context.Response.Headers[CorrelationHeader] = correlationId;
        using (LogContext.PushProperty("correlation_id", correlationId))
        using (LogContext.PushProperty("user_id", context.User?.Identity?.IsAuthenticated == true ? context.User.Identity?.Name : null))
        {
            await _next(context);
        }
    }
}
