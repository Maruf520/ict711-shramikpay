namespace ShramikPay.Infrastructure;

/// <summary>Maps domain exceptions to HTTP status codes; never returns stack traces.</summary>
public class ErrorHandling
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandling> _logger;

    public ErrorHandling(RequestDelegate next, ILogger<ErrorHandling> logger)
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
        catch (Exception ex) when (Map(ex) is int status)
        {
            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Unhandled error");
            }
            context.Response.StatusCode = status;
            var message = status == StatusCodes.Status500InternalServerError ? "Internal server error" : ex.Message;
            await context.Response.WriteAsJsonAsync(new { error = message });
        }
    }

    private static int? Map(Exception ex) => ex switch
    {
        KeyNotFoundException => StatusCodes.Status404NotFound,
        UnauthorizedAccessException => StatusCodes.Status403Forbidden,
        ArgumentException or FormatException or BadHttpRequestException => StatusCodes.Status400BadRequest,
        InvalidOperationException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
