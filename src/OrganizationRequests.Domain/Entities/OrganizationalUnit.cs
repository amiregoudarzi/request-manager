namespace OrganizationRequests.Domain.Entities;

public sealed class OrganizationalUnit : Entity
{
    private OrganizationalUnit() { }

    public OrganizationalUnit(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}

public sealed class UnitReviewer : Entity
{
    private UnitReviewer() { }

    public UnitReviewer(Guid userId, Guid organizationalUnitId)
    {
        if (userId == Guid.Empty || organizationalUnitId == Guid.Empty)
            throw new ArgumentException("User and organizational unit are required.");
        UserId = userId;
        OrganizationalUnitId = organizationalUnitId;
    }

    public Guid UserId { get; private set; }
    public Guid OrganizationalUnitId { get; private set; }
}
