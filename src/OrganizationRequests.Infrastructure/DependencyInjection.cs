using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrganizationRequests.Application;
using OrganizationRequests.Domain.Entities;
using OrganizationRequests.Infrastructure.Persistence;
using OrganizationRequests.Infrastructure.Security;

namespace OrganizationRequests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IDistributedLock, SqlDistributedLock>();
        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddIdentityCore<Account>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddClaimsPrincipalFactory<AccountClaimsFactory>();
        return services;
    }

    // Explicit, development-only bootstrap invoked with --seed; never runs during normal startup.
    public static async Task SeedDevelopmentAsync(this IServiceProvider services, string password, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var employee = await EnsureAccountAsync("employee", "Demo Employee");
        var reviewer = await EnsureAccountAsync("reviewer", "Demo Reviewer");
        var unit = await db.OrganizationalUnits.SingleOrDefaultAsync(x => x.Name == "IT", ct);
        if (unit is null)
        {
            unit = new OrganizationalUnit("IT");
            db.OrganizationalUnits.Add(unit);
        }
        await EnsureEmployeeAsync(employee, unit);
        await EnsureEmployeeAsync(reviewer, unit);
        await db.SaveChangesAsync(ct);
        if (!await db.UnitReviewers.AnyAsync(x => x.UserId == reviewer.Id && x.OrganizationalUnitId == unit.Id, ct))
            db.UnitReviewers.Add(new UnitReviewer(reviewer.Id, unit.Id));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        async Task EnsureEmployeeAsync(Account account, OrganizationalUnit destination)
        {
            var existing = await db.Employees.SingleOrDefaultAsync(x => x.UserId == account.Id, ct);
            if (existing is null)
                db.Employees.Add(new Employee(account.Id, destination.Id, account.DisplayName));
            else if (existing.OrganizationalUnitId != destination.Id)
                throw new InvalidOperationException("Demo account already belongs to a different unit.");
        }

        async Task<Account> EnsureAccountAsync(string username, string displayName)
        {
            var existing = await users.FindByNameAsync(username);
            if (existing is not null) return existing;
            var account = new Account { UserName = username, DisplayName = displayName };
            var result = await users.CreateAsync(account, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            return account;
        }
    }
}
