using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;

namespace OrganizationRequests.Web.Modules.Units.List;

public sealed class Query : ICommand<Query.Response>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public sealed record Item(Guid Id, string Name);
    public sealed record Response(int Page, int PageSize, int TotalCount, IReadOnlyList<Item> Items);

    public sealed class QueryHandler(IAppDbContext db) : ICommandHandler<Query, Response>
    {
        public async Task<Response> ExecuteAsync(Query query, CancellationToken ct)
        {
            var units = db.OrganizationalUnits.AsNoTracking().Where(x => x.IsActive);
            var total = await units.CountAsync(ct);
            var items = await units.OrderBy(x => x.Name).ThenBy(x => x.Id)
                .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
                .Select(x => new Item(x.Id, x.Name)).ToArrayAsync(ct);
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
    }
}
