using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OrganizationRequests.Application;
using IAuthenticationService = OrganizationRequests.Application.IAuthenticationService;

namespace OrganizationRequests.Web.Modules.Auth.Login;

public sealed class Command : ICommand<Command.Response>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public sealed record Response(Guid UserId, string Username, IReadOnlyList<string> Roles);

    public sealed class CommandHandler(IAuthenticationService authentication, IHttpContextAccessor accessor)
        : ICommandHandler<Command, Response>
    {
        public async Task<Response> ExecuteAsync(Command command, CancellationToken ct)
        {
            var principal = await authentication.AuthenticateAsync(command.Username, command.Password, ct)
                ?? throw new RequestException(401, "Invalid username or password.");
            var context = accessor.HttpContext!;
            await context.SignInAsync(IdentityConstants.ApplicationScheme, principal,
                new AuthenticationProperties { IsPersistent = false, AllowRefresh = false });
            context.Response.Headers.CacheControl = "no-store";
            return new Response(Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
                principal.Identity!.Name!, principal.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray());
        }
    }
}

public sealed class RequestValidator : Validator<Command>
{
    public RequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(1024);
    }
}
