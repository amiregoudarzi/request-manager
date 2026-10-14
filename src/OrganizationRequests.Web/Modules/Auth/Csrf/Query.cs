using FastEndpoints;
using Microsoft.AspNetCore.Antiforgery;

namespace OrganizationRequests.Web.Modules.Auth.Csrf;

public sealed class Query : ICommand<Query.Response>
{
    public sealed record Response(string Token);

    public sealed class QueryHandler(IAntiforgery antiforgery, IHttpContextAccessor accessor)
        : ICommandHandler<Query, Response>
    {
        public Task<Response> ExecuteAsync(Query query, CancellationToken ct)
        {
            var context = accessor.HttpContext!;
            context.Response.Headers.CacheControl = "no-store";
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Task.FromResult(new Response(tokens.RequestToken!));
        }
    }
}
