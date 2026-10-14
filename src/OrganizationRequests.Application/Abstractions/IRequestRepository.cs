using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application.Abstractions;

public interface IRequestRepository
{
    IQueryable<ServiceRequest> VisibleTo(Guid userId);
    // Null means the creator has no employee membership.
    Task<RequestCreationContext?> GetCreationContextAsync(Guid creatorId, Guid unitId,
        Guid? receiverEmployeeId, CancellationToken ct);
    Task<bool> HasReviewerAsync(Guid userId, Guid unitId, CancellationToken ct);
    // Acquire the request lock before reading; requires ExecuteInTransactionAsync.
    Task<ServiceRequest?> GetForReviewAsync(Guid requestId, CancellationToken ct);
    void Add(ServiceRequest request);
    // Save all tracked changes and commit only when the operation succeeds.
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
}
