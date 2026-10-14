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
