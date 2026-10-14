
namespace OrganizationRequests.Application;

public interface ICurrentUser
{
    Guid Id { get; }
    bool IsUnitReviewer { get; }
}
