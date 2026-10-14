using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application;

public interface IAppDbContext
{
    DbSet<OrganizationalUnit> OrganizationalUnits { get; }
    DbSet<UnitReviewer> UnitReviewers { get; }
    DbSet<ServiceRequest> Requests { get; }
    DbSet<RequestStatusHistory> RequestStatusHistories { get; }
}

public interface ICurrentUser
{
    Guid Id { get; }
    bool IsReviewer { get; }
}

public interface IAuthenticationService
{
    Task<ClaimsPrincipal?> AuthenticateAsync(string username, string password, CancellationToken ct);
}

public interface IDistributedLock
{
    // Requires an active database transaction; released by commit or rollback.
    Task AcquireAsync(string resource, CancellationToken ct);
}

public interface IRequestRepository
{
    IQueryable<ServiceRequest> VisibleTo(Guid userId, bool isReviewer);
    Task<ServiceRequest> CreateAsync(string title, string description, RequestType type,
        Guid creatorId, Guid destinationUnitId, CancellationToken ct);
    Task<ServiceRequest> ReviewAsync(Guid requestId, Guid actorId, bool isReviewer,
        RequestStatus result, string? comment, CancellationToken ct);
}
