using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Application;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Infrastructure.Persistence;

public sealed class RequestRepository(AppDbContext db, IDistributedLock distributedLock) : IRequestRepository
{
    public IQueryable<ServiceRequest> VisibleTo(Guid userId) =>
        db.Requests.AsNoTracking().Where(request => request.CreatedById == userId ||
            db.Employees.Any(employee => employee.UserId == userId &&
                employee.OrganizationalUnitId == request.OrganizationalUnitId));

    public async Task<ServiceRequest> CreateAsync(string title, string description, RequestType type,
        Guid creatorId, Guid destinationUnitId, Guid? receiverEmployeeId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            if (!await db.Employees.AnyAsync(x => x.UserId == creatorId, ct))
                throw new RequestException(403, "Your account must be assigned to an organizational unit first.");
            if (!await db.OrganizationalUnits.AnyAsync(x => x.Id == destinationUnitId && x.IsActive, ct))
                throw new RequestException(404, "Destination unit was not found or is inactive.");
            if (receiverEmployeeId.HasValue && !await db.Employees.AnyAsync(x => x.Id == receiverEmployeeId.Value &&
                    x.OrganizationalUnitId == destinationUnitId, ct))
                throw new RequestException(400, "Receiver must be an employee of the destination unit.");

            var request = new ServiceRequest(title, description, type, creatorId, destinationUnitId, receiverEmployeeId);
            db.Requests.Add(request);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return request;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<ServiceRequest> ReviewAsync(Guid requestId, Guid actorId, bool isUnitReviewer,
        RequestStatus result, string? comment, CancellationToken ct)
    {
        if (!isUnitReviewer) throw new RequestException(403, "UnitReviewer permission is required.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // All instances use the same resource in the same database transaction.
            await distributedLock.AcquireAsync($"request:{requestId:N}", ct);
            var request = await db.Requests.SingleOrDefaultAsync(x => x.Id == requestId, ct)
                ?? throw new RequestException(404, "Request was not found.");
            if (!await db.UnitReviewers.AnyAsync(x => x.UserId == actorId &&
                    x.OrganizationalUnitId == request.OrganizationalUnitId, ct) ||
                !await db.Employees.AnyAsync(x => x.UserId == actorId &&
                    x.OrganizationalUnitId == request.OrganizationalUnitId, ct))
                throw new RequestException(403, "You cannot review requests for this destination unit.");
            if (request.Status != RequestStatus.Pending)
                throw new RequestException(409, "The request has already been reviewed.");

            request.Review(result, actorId, comment);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return request;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
