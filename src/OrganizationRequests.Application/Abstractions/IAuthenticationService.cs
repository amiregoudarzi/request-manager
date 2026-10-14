using System.Security.Claims;

namespace OrganizationRequests.Application.Abstractions;

public interface IAuthenticationService
{
    Task<ClaimsPrincipal?> AuthenticateAsync(string username, string password, CancellationToken ct);
    Task<Guid> RegisterAsync(string username, string password, string displayName, CancellationToken ct);
}
