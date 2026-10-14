using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using OrganizationRequests.Infrastructure.Persistence;

namespace OrganizationRequests.Infrastructure.Security;

// Delegate persistence to the standard EF store, exposing only supported capabilities.
// Claims, external logins and persisted tokens are deliberately not advertised to Identity.
public sealed class AccountStore(AppDbContext db, IdentityErrorDescriber errors) :
    IUserPasswordStore<Account>, IUserSecurityStampStore<Account>,
    IUserLockoutStore<Account>, IUserTwoFactorStore<Account>
{
    private readonly UserOnlyStore<Account, AppDbContext, Guid> _store = new(db, errors);

    public Task<IdentityResult> CreateAsync(Account user, CancellationToken ct) =>
        _store.CreateAsync(user, ct);

    public Task<IdentityResult> UpdateAsync(Account user, CancellationToken ct) =>
        _store.UpdateAsync(user, ct);

    public Task<IdentityResult> DeleteAsync(Account user, CancellationToken ct) =>
        _store.DeleteAsync(user, ct);

    public Task<Account?> FindByIdAsync(string userId, CancellationToken ct) =>
        _store.FindByIdAsync(userId, ct);

    public Task<Account?> FindByNameAsync(string normalizedUserName, CancellationToken ct) =>
        _store.FindByNameAsync(normalizedUserName, ct);

    public Task<string> GetUserIdAsync(Account user, CancellationToken ct) =>
        _store.GetUserIdAsync(user, ct);

    public Task<string?> GetUserNameAsync(Account user, CancellationToken ct) =>
        _store.GetUserNameAsync(user, ct);

    public Task SetUserNameAsync(Account user, string? userName, CancellationToken ct) =>
        _store.SetUserNameAsync(user, userName, ct);

    public Task<string?> GetNormalizedUserNameAsync(Account user, CancellationToken ct) =>
        _store.GetNormalizedUserNameAsync(user, ct);

    public Task SetNormalizedUserNameAsync(Account user, string? normalizedName, CancellationToken ct) =>
        _store.SetNormalizedUserNameAsync(user, normalizedName, ct);

    public Task SetPasswordHashAsync(Account user, string? passwordHash, CancellationToken ct) =>
        _store.SetPasswordHashAsync(user, passwordHash, ct);

    public Task<string?> GetPasswordHashAsync(Account user, CancellationToken ct) =>
        _store.GetPasswordHashAsync(user, ct);

    public Task<bool> HasPasswordAsync(Account user, CancellationToken ct) =>
        _store.HasPasswordAsync(user, ct);

    public Task SetSecurityStampAsync(Account user, string stamp, CancellationToken ct) =>
        _store.SetSecurityStampAsync(user, stamp, ct);

    public Task<string?> GetSecurityStampAsync(Account user, CancellationToken ct) =>
        _store.GetSecurityStampAsync(user, ct);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(Account user, CancellationToken ct) =>
        _store.GetLockoutEndDateAsync(user, ct);

    public Task SetLockoutEndDateAsync(Account user, DateTimeOffset? lockoutEnd, CancellationToken ct) =>
        _store.SetLockoutEndDateAsync(user, lockoutEnd, ct);

    public Task<int> IncrementAccessFailedCountAsync(Account user, CancellationToken ct) =>
        _store.IncrementAccessFailedCountAsync(user, ct);

    public Task ResetAccessFailedCountAsync(Account user, CancellationToken ct) =>
        _store.ResetAccessFailedCountAsync(user, ct);

    public Task<int> GetAccessFailedCountAsync(Account user, CancellationToken ct) =>
        _store.GetAccessFailedCountAsync(user, ct);

    public Task<bool> GetLockoutEnabledAsync(Account user, CancellationToken ct) =>
        _store.GetLockoutEnabledAsync(user, ct);

    public Task SetLockoutEnabledAsync(Account user, bool enabled, CancellationToken ct) =>
        _store.SetLockoutEnabledAsync(user, enabled, ct);

    public Task SetTwoFactorEnabledAsync(Account user, bool enabled, CancellationToken ct) =>
        _store.SetTwoFactorEnabledAsync(user, enabled, ct);

    public Task<bool> GetTwoFactorEnabledAsync(Account user, CancellationToken ct) =>
        _store.GetTwoFactorEnabledAsync(user, ct);

    public void Dispose() => _store.Dispose();
}
