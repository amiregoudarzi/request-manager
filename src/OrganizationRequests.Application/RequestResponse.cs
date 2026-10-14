using System.Linq.Expressions;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application;

public sealed record RequestResponse(Guid Id, string Title, string Description, RequestType Type,
    Guid CreatedById, Guid OrganizationalUnitId, Guid? ReceiverEmployeeId, RequestStatus Status, DateTime CreatedDateUtc)
{
    public static Expression<Func<ServiceRequest, RequestResponse>> Projection => request =>
        new RequestResponse(request.Id, request.Title, request.Description, request.Type,
            request.CreatedById, request.OrganizationalUnitId, request.ReceiverEmployeeId, request.Status, request.CreatedDateUtc);

    public static RequestResponse From(ServiceRequest request) =>
        new(request.Id, request.Title, request.Description, request.Type, request.CreatedById,
            request.OrganizationalUnitId, request.ReceiverEmployeeId, request.Status, request.CreatedDateUtc);
}
