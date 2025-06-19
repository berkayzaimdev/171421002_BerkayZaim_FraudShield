using System.Text;

namespace Analiz.API.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = await FormatRequest(context.Request);
        _logger.LogInformation($"Request: {request}");

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var startTime = DateTime.UtcNow;
        await _next(context);
        var duration = DateTime.UtcNow - startTime;

        var response = await FormatResponse(context.Response);
        _logger.LogInformation($"Response: {response}, Duration: {duration.TotalMilliseconds}ms");

        await responseBody.CopyToAsync(originalBodyStream);
    }

    private static async Task<string> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();

        var bodyStr = string.Empty;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
        {
            bodyStr = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        return $"HTTP {request.Method} {request.Path}{request.QueryString} " +
               $"| Headers: {string.Join(", ", request.Headers.Select(h => $"{h.Key}={h.Value}"))} " +
               $"| Body: {(string.IsNullOrEmpty(bodyStr) ? "empty" : bodyStr)}";
    }

    private static async Task<string> FormatResponse(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var bodyText = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        return $"StatusCode: {response.StatusCode} " +
               $"| Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={h.Value}"))} " +
               $"| Body: {(string.IsNullOrEmpty(bodyText) ? "empty" : bodyText)}";
    }
}