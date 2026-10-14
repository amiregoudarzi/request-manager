
namespace OrganizationRequests.Application;

public interface IDistributedLock
{
    // Requires an active database transaction; released by commit or rollback.
    Task AcquireAsync(string resource, CancellationToken ct);
}
