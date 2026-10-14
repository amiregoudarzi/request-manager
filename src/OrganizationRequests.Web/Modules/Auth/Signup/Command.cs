using FastEndpoints;
using FluentValidation;
using OrganizationRequests.Application.Abstractions;

namespace OrganizationRequests.Web.Modules.Auth.Signup;

public sealed class Command : ICommand<Command.Response>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public sealed record Response(Guid UserId, string Username);

    public sealed class CommandHandler(IAuthenticationService authentication)
        : ICommandHandler<Command, Response>
    {
        public async Task<Response> ExecuteAsync(Command command, CancellationToken ct)
        {
            var id = await authentication.RegisterAsync(command.Username, command.Password, command.DisplayName, ct);
            return new Response(id, command.Username.Trim());
        }
    }
}

public sealed class RequestValidator : Validator<Command>
{
    public RequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(1024);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}
