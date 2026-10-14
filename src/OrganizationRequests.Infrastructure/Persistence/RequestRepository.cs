using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Infrastructure.Persistence;

public sealed class RequestRepository(AppDbContext db, IDistributedLock distributedLock) : IRequestRepository
{
    public IQueryable<ServiceRequest> VisibleTo(Guid userId) =>
        db.Requests.AsNoTracking().Where(request => request.CreatedById == userId ||
            db.Employees.Any(employee => employee.UserId == userId &&
                employee.OrganizationalUnitId == request.OrganizationalUnitId));

    public Task<bool> HasEmployeeAsync(Guid userId, CancellationToken ct) =>
        db.Employees.AnyAsync(x => x.UserId == userId, ct);

    public Task<bool> HasActiveUnitAsync(Guid unitId, CancellationToken ct) =>
        db.OrganizationalUnits.AnyAsync(x => x.Id == unitId && x.IsActive, ct);

    public Task<bool> HasReceiverAsync(Guid employeeId, Guid unitId, CancellationToken ct) =>
        db.Employees.AnyAsync(x => x.Id == employeeId && x.OrganizationalUnitId == unitId, ct);

    public Task<bool> HasReviewerAsync(Guid userId, Guid unitId, CancellationToken ct) =>
        db.UnitReviewers.AnyAsync(x => x.UserId == userId && x.OrganizationalUnitId == unitId, ct);

    public async Task<ServiceRequest?> GetForReviewAsync(Guid requestId, CancellationToken ct)
    {
        await distributedLock.AcquireAsync($"request:{requestId:N}", ct);
        return await db.Requests.SingleOrDefaultAsync(x => x.Id == requestId, ct);
    }

    public void Add(ServiceRequest request) => db.Requests.Add(request);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operation(ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
