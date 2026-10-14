using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application;

public interface IRequestRepository
{
    IQueryable<ServiceRequest> VisibleTo(Guid userId);
    Task<ServiceRequest> CreateAsync(string title, string description, RequestType type,
        Guid creatorId, Guid destinationUnitId, Guid? receiverEmployeeId, CancellationToken ct);
    Task<ServiceRequest> ReviewAsync(Guid requestId, Guid actorId, bool isUnitReviewer,
        RequestStatus result, string? comment, CancellationToken ct);
}
