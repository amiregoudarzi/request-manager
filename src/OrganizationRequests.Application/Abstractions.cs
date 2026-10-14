using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application;







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
