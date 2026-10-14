using FastEndpoints;
using FluentValidation;
using OrganizationRequests.Application;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Web.Modules.Requests.Approve;

public sealed class Command : ICommand<RequestResponse>
{
    public Guid Id { get; set; }
    public string? Comment { get; set; }

    public sealed class CommandHandler(IRequestRepository repository, ICurrentUser user)
        : ICommandHandler<Command, RequestResponse>
    {
        public async Task<RequestResponse> ExecuteAsync(Command command, CancellationToken ct)
        {
            var request = await repository.ReviewAsync(command.Id, user.Id, user.IsReviewer,
                RequestStatus.Approved, command.Comment, ct);
            return RequestResponse.From(request);
        }
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
