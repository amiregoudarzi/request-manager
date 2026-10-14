namespace OrganizationRequests.Domain.Entities;

public sealed class RequestStatusHistory : Entity
{
    private RequestStatusHistory() { }

    internal RequestStatusHistory(Guid requestId, RequestStatus? previousStatus,
        RequestStatus newStatus, Guid actorId, string? comment)
    {
        RequestId = requestId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ActorId = actorId;
        Comment = comment;
    }

    public Guid RequestId { get; private set; }
    public RequestStatus? PreviousStatus { get; private set; }
    public RequestStatus NewStatus { get; private set; }
    public Guid ActorId { get; private set; }
    public string? Comment { get; private set; }
}
