using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Application;

namespace OrganizationRequests.Web.Modules.Requests.Get;

public sealed class Query : ICommand<RequestResponse>
{
    public Guid Id { get; set; }

    public sealed class QueryHandler(IRequestRepository repository, ICurrentUser user)
        : ICommandHandler<Query, RequestResponse>
    {
        public async Task<RequestResponse> ExecuteAsync(Query query, CancellationToken ct) =>
            await repository.VisibleTo(user.Id, user.IsReviewer).Where(x => x.Id == query.Id)
                .Select(RequestResponse.Projection).SingleOrDefaultAsync(ct)
            ?? throw new RequestException(404, "Request was not found.");
    }
}

public sealed class RequestValidator : Validator<Query>
{
    public RequestValidator() { RuleFor(x => x.Id).NotEmpty(); }
}
