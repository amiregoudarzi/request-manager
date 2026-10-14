using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrganizationRequests.Application.Abstractions;
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
}
