using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace OrganizationRequests.Web.Modules.Auth.Logout;

public sealed class Command : ICommand<Command.Response>
{
    public sealed record Response(string Message);

    public sealed class CommandHandler(IHttpContextAccessor accessor) : ICommandHandler<Command, Response>
    {
        public async Task<Response> ExecuteAsync(Command command, CancellationToken ct)
        {
            await accessor.HttpContext!.SignOutAsync(IdentityConstants.ApplicationScheme);
            return new Response("Signed out.");
        }
    }
}
