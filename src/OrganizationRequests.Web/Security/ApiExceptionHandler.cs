using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Application;

namespace OrganizationRequests.Web.Security;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return false;
        if (context.Response.HasStarted)
            return false;
        var (status, detail) = exception switch
        {
            RequestException error when error.StatusCode is >= 400 and < 500 => (error.StatusCode, error.Message),
            DbUpdateConcurrencyException => (409, "The request changed during processing. Reload its current state."),
            AntiforgeryValidationException => (400, "A valid X-CSRF-TOKEN and matching cookie are required."),
            BadHttpRequestException error => (error.StatusCode, "The HTTP request could not be processed."),
            _ => (500, "An unexpected error occurred.")
        };
        if (status == 500) logger.LogError(exception, "Request failed. TraceId: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, options: null, contentType: "application/problem+json", cancellationToken: ct);
        return true;
    }
}
