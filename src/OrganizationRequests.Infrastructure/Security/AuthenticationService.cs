using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;

namespace OrganizationRequests.Infrastructure.Security;

public sealed class AuthenticationService(UserManager<Account> users, SignInManager<Account> signIn)
    : IAuthenticationService
{
    public async Task<Guid> RegisterAsync(string username, string password, string displayName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var account = new Account { UserName = username.Trim(), DisplayName = displayName.Trim() };
        try
        {
            var result = await users.CreateAsync(account, password);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
                    throw new RequestException(409, "Username is already registered.");
                throw new RequestException(400, string.Join(" ", result.Errors.Select(x => x.Description)));
            }
            return account.Id;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new RequestException(409, "Username is already registered.");
        }
    }

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
