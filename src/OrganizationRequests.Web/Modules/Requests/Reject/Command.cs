using FastEndpoints;
using FluentValidation;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Web.Modules.Requests.Reject;

public sealed class Command : ICommand<RequestResponse>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;

    public sealed class CommandHandler(IRequestService service, ICurrentUser user)
        : ICommandHandler<Command, RequestResponse>
    {
        public Task<RequestResponse> ExecuteAsync(Command command, CancellationToken ct) =>
            service.ReviewAsync(command.Id, user.Id,
                RequestStatus.Rejected, command.Reason, ct);
    }
}

public sealed class RequestValidator : Validator<Command>
{
    public RequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
