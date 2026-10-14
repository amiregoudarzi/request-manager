namespace OrganizationRequests.Domain.Entities;

public sealed class Employee : Entity
{
    private Employee() { }

    public Employee(Guid userId, Guid organizationalUnitId, string name)
    {
        if (userId == Guid.Empty || organizationalUnitId == Guid.Empty)
            throw new ArgumentException("A user and organizational unit are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        UserId = userId;
        OrganizationalUnitId = organizationalUnitId;
        Name = name.Trim();
    }

    public Guid UserId { get; private set; }
    public Guid OrganizationalUnitId { get; private set; }
    public string Name { get; private set; } = string.Empty;
}
