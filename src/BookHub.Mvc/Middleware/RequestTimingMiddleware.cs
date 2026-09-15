using System.Diagnostics;

namespace BookHub.Mvc.Middleware;

// Middleware is just a class with an InvokeAsync(HttpContext) method by convention --
// no interface to implement. The DI container constructs one instance at startup and
// reuses it for every request, so don't store per-request state on `this` (fields on
// this class are shared/mutable across concurrent requests -- use locals instead).
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    // The DI container hands us the "rest of the pipeline" as a delegate,
    // plus anything else we ask for in the constructor (here, a logger).
    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Everything before this line runs on the way IN (like the top half of a
        // Python `with` block / context manager __enter__).
        await _next(context);
        // Everything after this line runs on the way OUT, once every later
        // middleware/controller has already produced a response (__exit__).

        stopwatch.Stop();
        _logger.LogInformation(
            "{Method} {Path} -> {StatusCode} ({ElapsedMs}ms)",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}

// Extension method so Program.cs can say `app.UseRequestTiming()` instead of the
// more mechanical `app.UseMiddleware<RequestTimingMiddleware>()`. Purely cosmetic,
// but it's the idiomatic pattern every built-in `Use...` method also follows.
public static class RequestTimingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder app)
        => app.UseMiddleware<RequestTimingMiddleware>();
}
