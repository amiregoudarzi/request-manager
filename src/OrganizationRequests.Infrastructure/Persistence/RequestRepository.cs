using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;
using OrganizationRequests.Domain.Entities;
using OrganizationRequests.Domain.Enums;

namespace OrganizationRequests.Infrastructure.Persistence;

public sealed class RequestRepository(AppDbContext db, IDistributedLock distributedLock) : IRequestRepository
{
    public IQueryable<ServiceRequest> VisibleTo(Guid userId) =>
        db.Requests.AsNoTracking().Where(request => request.CreatedById == userId ||
            db.Employees.Any(employee => employee.UserId == userId &&
                employee.OrganizationalUnitId == request.OrganizationalUnitId));

    public Task<RequestCreationContext?> GetCreationContextAsync(Guid creatorId, Guid unitId,
        Guid? receiverEmployeeId, CancellationToken ct) =>
        db.Employees.AsNoTracking().Where(x => x.UserId == creatorId)
            .Select(x => new RequestCreationContext(
                db.OrganizationalUnits.Any(unit => unit.Id == unitId && unit.IsActive),
                !receiverEmployeeId.HasValue || db.Employees.Any(receiver =>
                    receiver.Id == receiverEmployeeId.Value && receiver.OrganizationalUnitId == unitId)))
            .SingleOrDefaultAsync(ct);

    public Task<bool> HasReviewerAsync(Guid userId, Guid unitId, CancellationToken ct) =>
        db.Employees.AnyAsync(x => x.UserId == userId && x.OrganizationalUnitId == unitId &&
            db.Users.Any(account => account.Id == x.UserId && account.IsActive && account.Role == UserRole.UnitReviewer), ct);

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
