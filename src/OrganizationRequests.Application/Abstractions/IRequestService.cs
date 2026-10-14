using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application.Abstractions;

public interface IRequestService
{
    Task<RequestResponse> CreateAsync(string title, string description, RequestType type,
        Guid creatorId, Guid destinationUnitId, Guid? receiverEmployeeId, CancellationToken ct);
    Task<RequestResponse> ReviewAsync(Guid requestId, Guid actorId,
        RequestStatus result, string? comment, CancellationToken ct);
}
