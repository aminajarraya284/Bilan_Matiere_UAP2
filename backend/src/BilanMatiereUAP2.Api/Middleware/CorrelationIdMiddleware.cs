using System.Text.RegularExpressions;
using Serilog.Context;

namespace BilanMatiereUAP2.Api.Middleware;

public sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "Sopal.CorrelationId";
    public const int MaxLength = 100;

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault()?.Trim();
        var correlationId = IsValid(supplied) ? supplied! : Guid.NewGuid().ToString("N");
        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.Length <= MaxLength && SafeValueRegex().IsMatch(value);

    [GeneratedRegex("^[A-Za-z0-9._:-]+$")]
    private static partial Regex SafeValueRegex();
}
