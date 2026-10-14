using FastEndpoints;
using FluentValidation;
using OrganizationRequests.Application;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Web.Modules.Requests.Reject;

public sealed class Command : ICommand<RequestResponse>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;

    public sealed class CommandHandler(IRequestRepository repository, ICurrentUser user)
        : ICommandHandler<Command, RequestResponse>
    {
        public async Task<RequestResponse> ExecuteAsync(Command command, CancellationToken ct)
        {
            var request = await repository.ReviewAsync(command.Id, user.Id, user.IsReviewer,
                RequestStatus.Rejected, command.Reason, ct);
            return RequestResponse.From(request);
        }
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
