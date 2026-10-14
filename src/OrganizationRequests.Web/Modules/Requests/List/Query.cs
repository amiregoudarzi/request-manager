using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Web.Modules.Requests.List;

public sealed class Query : ICommand<Query.Response>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public RequestStatus? Status { get; set; }
    public RequestType? Type { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
    public sealed record Response(int Page, int PageSize, int TotalCount, IReadOnlyList<RequestResponse> Items);

    public sealed class QueryHandler(IRequestRepository repository, ICurrentUser user)
        : ICommandHandler<Query, Response>
    {
        public async Task<Response> ExecuteAsync(Query query, CancellationToken ct)
        {
            var requests = repository.VisibleTo(user.Id);
            if (query.Status.HasValue) requests = requests.Where(x => x.Status == query.Status.Value);
            if (query.Type.HasValue) requests = requests.Where(x => x.Type == query.Type.Value);
            if (query.OrganizationalUnitId.HasValue)
                requests = requests.Where(x => x.OrganizationalUnitId == query.OrganizationalUnitId.Value);
            var total = await requests.CountAsync(ct);
            if (total == 0)
                return new Response(query.Page, query.PageSize, total, []);
            var items = await requests.OrderByDescending(x => x.CreatedDateUtc).ThenByDescending(x => x.Id)
                .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
                .Select(RequestResponse.Projection).ToArrayAsync(ct);
            return new Response(query.Page, query.PageSize, total, items);
        }
    }
}

public sealed class RequestValidator : Validator<Query>
{
    public RequestValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 10_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.OrganizationalUnitId).NotEqual(Guid.Empty).When(x => x.OrganizationalUnitId.HasValue);
    }
}
