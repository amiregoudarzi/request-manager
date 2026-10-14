using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OrganizationRequests.Domain.Enums;

namespace OrganizationRequests.Infrastructure.Security;

public sealed class AccountClaimsFactory(UserManager<Account> users, IOptions<IdentityOptions> options) : UserClaimsPrincipalFactory<Account>(users, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Account user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        // A reviewer also inherits employee permissions.
        foreach (var claim in identity.FindAll(Options.ClaimsIdentity.RoleClaimType).ToArray())
            identity.RemoveClaim(claim);
        identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, nameof(UserRole.Employee)));
        if (user.Role == UserRole.UnitReviewer)
            identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, nameof(UserRole.UnitReviewer)));
        return identity;
    }
}
