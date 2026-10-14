namespace OrganizationRequests.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreatedDateUtc { get; private set; } = DateTime.UtcNow;
}
