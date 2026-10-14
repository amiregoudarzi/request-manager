using System.Security.Claims;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;

namespace OrganizationRequests.Web.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid Id => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new RequestException(401, "Authentication is required.");
}
