using FastEndpoints;
using FluentValidation;
using OrganizationRequests.Application;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Web.Modules.Requests.Create;

public sealed class Command : ICommand<RequestResponse>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RequestType? Type { get; set; }
    public Guid OrganizationalUnitId { get; set; }

    public sealed class CommandHandler(IRequestRepository repository, ICurrentUser user)
        : ICommandHandler<Command, RequestResponse>
    {
        public async Task<RequestResponse> ExecuteAsync(Command command, CancellationToken ct)
        {
            var request = await repository.CreateAsync(command.Title, command.Description, command.Type!.Value,
                user.Id, command.OrganizationalUnitId, ct);
            return RequestResponse.From(request);
        }
    }
}

public sealed class RequestValidator : Validator<Command>
{
    public RequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Type).NotNull().IsInEnum();
        RuleFor(x => x.OrganizationalUnitId).NotEmpty();
    }
}
