using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application;

public interface IAppDbContext
{
    DbSet<OrganizationalUnit> OrganizationalUnits { get; }
    DbSet<UnitReviewer> UnitReviewers { get; }
    DbSet<Employee> Employees { get; }
    DbSet<ServiceRequest> Requests { get; }
    DbSet<RequestStatusHistory> RequestStatusHistories { get; }
}

public interface ICurrentUser
{
    Guid Id { get; }
    bool IsUnitReviewer { get; }
}

public interface IAuthenticationService
{
    Task<ClaimsPrincipal?> AuthenticateAsync(string username, string password, CancellationToken ct);
    Task<Guid> RegisterAsync(string username, string password, string displayName, CancellationToken ct);
}

public interface IDistributedLock
{
    // Requires an active database transaction; released by commit or rollback.
    Task AcquireAsync(string resource, CancellationToken ct);
}

public interface IRequestRepository
{
    IQueryable<ServiceRequest> VisibleTo(Guid userId);
    Task<ServiceRequest> CreateAsync(string title, string description, RequestType type,
        Guid creatorId, Guid destinationUnitId, Guid? receiverEmployeeId, CancellationToken ct);
    Task<ServiceRequest> ReviewAsync(Guid requestId, Guid actorId, bool isUnitReviewer,
        RequestStatus result, string? comment, CancellationToken ct);
}
