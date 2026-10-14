using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<OrganizationalUnit> OrganizationalUnits { get; }
    DbSet<Employee> Employees { get; }
    DbSet<ServiceRequest> Requests { get; }
    DbSet<RequestStatusHistory> RequestStatusHistories { get; }
}
