using Microsoft.AspNetCore.Identity;

namespace OrganizationRequests.Infrastructure.Security;

// Identity owns this persistence model; business entities keep the common Entity base.
public sealed class Account : IdentityUser<Guid>
{
    public Account() { Id = Guid.NewGuid(); }
    public DateTime CreatedDateUtc { get; private set; } = DateTime.UtcNow;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
