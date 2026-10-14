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
