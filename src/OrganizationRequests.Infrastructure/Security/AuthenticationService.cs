using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrganizationRequests.Application;
using OrganizationRequests.Infrastructure.Persistence;

namespace OrganizationRequests.Infrastructure.Security;

// Identity owns this persistence model; business entities keep the common Entity base.
public sealed class Account : IdentityUser<Guid>
{
    public Account() { Id = Guid.NewGuid(); }
    public DateTime CreatedDateUtc { get; private set; } = DateTime.UtcNow;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class AuthenticationService(UserManager<Account> users, SignInManager<Account> signIn)
    : IAuthenticationService
{
    public async Task<ClaimsPrincipal?> AuthenticateAsync(string username, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var account = await users.FindByNameAsync(username.Trim());
        if (account is null || !account.IsActive || account.TwoFactorEnabled) return null;

        var result = await signIn.CheckPasswordSignInAsync(account, password, lockoutOnFailure: true);
        if (!result.Succeeded) return null;
        return await signIn.CreateUserPrincipalAsync(account);
    }
}

public sealed class AccountClaimsFactory(UserManager<Account> users, IOptions<IdentityOptions> options,
    AppDbContext db) : UserClaimsPrincipalFactory<Account>(users, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Account user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        // Roles are deliberately derived from current reviewer assignments.
        foreach (var claim in identity.FindAll(Options.ClaimsIdentity.RoleClaimType).ToArray())
            identity.RemoveClaim(claim);
        identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, "Employee"));
        if (await db.UnitReviewers.AsNoTracking().AnyAsync(x => x.UserId == user.Id))
            identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, "Reviewer"));
        return identity;
    }
}
