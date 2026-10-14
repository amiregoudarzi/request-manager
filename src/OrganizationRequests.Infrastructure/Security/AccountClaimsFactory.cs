using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrganizationRequests.Domain.Enums;
using OrganizationRequests.Infrastructure.Persistence;

namespace OrganizationRequests.Infrastructure.Security;

public sealed class AccountClaimsFactory(UserManager<Account> users, IOptions<IdentityOptions> options,
    AppDbContext db) : UserClaimsPrincipalFactory<Account>(users, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Account user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        // Roles are deliberately derived from current reviewer assignments.
        foreach (var claim in identity.FindAll(Options.ClaimsIdentity.RoleClaimType).ToArray())
            identity.RemoveClaim(claim);
        identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, nameof(UserRole.Employee)));
        if (await db.UnitReviewers.AsNoTracking().AnyAsync(x => x.UserId == user.Id))
            identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, nameof(UserRole.UnitReviewer)));
        return identity;
    }
}
