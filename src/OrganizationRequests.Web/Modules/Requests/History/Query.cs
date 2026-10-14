using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Web.Modules.Requests.History;

public sealed class Query : ICommand<Query.Response>
{
    public Guid Id { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public sealed record Item(Guid Id, RequestStatus? PreviousStatus, RequestStatus NewStatus,
        Guid ActorId, DateTime CreatedDateUtc, string? Comment);
    public sealed record Response(int Page, int PageSize, int TotalCount, IReadOnlyList<Item> Items);

    public sealed class QueryHandler(IAppDbContext db, IRequestRepository repository, ICurrentUser user)
        : ICommandHandler<Query, Response>
    {
        public async Task<Response> ExecuteAsync(Query query, CancellationToken ct)
        {
            var total = await repository.VisibleTo(user.Id).Where(x => x.Id == query.Id)
                .Select(request => (int?)db.RequestStatusHistories.Count(x => x.RequestId == request.Id))
                .SingleOrDefaultAsync(ct)
                ?? throw new RequestException(404, "Request was not found.");
            if (total == 0)
                return new Response(query.Page, query.PageSize, total, []);
            var history = db.RequestStatusHistories.AsNoTracking().Where(x => x.RequestId == query.Id);
            var items = await history.OrderBy(x => x.CreatedDateUtc).ThenBy(x => x.Id)
                .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
                .Select(x => new Item(x.Id, x.PreviousStatus, x.NewStatus, x.ActorId, x.CreatedDateUtc, x.Comment))
                .ToArrayAsync(ct);
            return new Response(query.Page, query.PageSize, total, items);
        }
    }
}

public sealed class RequestValidator : Validator<Query>
{
    public RequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Page).InclusiveBetween(1, 10_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
