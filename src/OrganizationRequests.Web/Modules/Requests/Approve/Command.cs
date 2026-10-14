using FastEndpoints;
using FluentValidation;
using OrganizationRequests.Application;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Web.Modules.Requests.Approve;

public sealed class Command : ICommand<RequestResponse>
{
    public Guid Id { get; set; }
    public string? Comment { get; set; }

    public sealed class CommandHandler(IRequestService service, ICurrentUser user)
        : ICommandHandler<Command, RequestResponse>
    {
        public Task<RequestResponse> ExecuteAsync(Command command, CancellationToken ct) =>
            service.ReviewAsync(command.Id, user.Id,
                RequestStatus.Approved, command.Comment, ct);
    }
}

public sealed class RequestValidator : Validator<Command>
{
    public RequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
