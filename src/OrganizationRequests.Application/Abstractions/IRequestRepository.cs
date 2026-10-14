using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application.Abstractions;

public interface IRequestRepository
{
    IQueryable<ServiceRequest> VisibleTo(Guid userId);
    Task<bool> HasEmployeeAsync(Guid userId, CancellationToken ct);
    Task<bool> HasActiveUnitAsync(Guid unitId, CancellationToken ct);
    Task<bool> HasReceiverAsync(Guid employeeId, Guid unitId, CancellationToken ct);
    Task<bool> HasReviewerAsync(Guid userId, Guid unitId, CancellationToken ct);
    // Acquire the request lock before reading; requires ExecuteInTransactionAsync.
    Task<ServiceRequest?> GetForReviewAsync(Guid requestId, CancellationToken ct);
    void Add(ServiceRequest request);
    // Save all tracked changes and commit only when the operation succeeds.
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
}
