namespace OrganizationRequests.Domain.Entities;

public enum RequestStatus { Pending, Approved, Rejected }
public enum RequestType { Equipment, InternalService, TechnicalIssue }

public sealed class ServiceRequest : Entity
{
    private readonly List<RequestStatusHistory> _history = [];
    private ServiceRequest() { }

    public ServiceRequest(string title, string description, RequestType type, Guid createdById, Guid unitId,
        Guid? receiverEmployeeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (!Enum.IsDefined(type) || createdById == Guid.Empty || unitId == Guid.Empty || receiverEmployeeId == Guid.Empty)
            throw new ArgumentException("Valid type, creator, destination unit and optional receiver are required.");
        Title = title.Trim();
        Description = description.Trim();
        Type = type;
        CreatedById = createdById;
        OrganizationalUnitId = unitId;
        ReceiverEmployeeId = receiverEmployeeId;
        _history.Add(new RequestStatusHistory(Id, null, RequestStatus.Pending, createdById, null));
    }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public RequestType Type { get; private set; }
    public Guid CreatedById { get; private set; }
    public Guid OrganizationalUnitId { get; private set; }
    // A request can target a unit without naming an individual receiver.
    public Guid? ReceiverEmployeeId { get; private set; }
    public RequestStatus Status { get; private set; } = RequestStatus.Pending;
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<RequestStatusHistory> History => _history;

    public void Review(RequestStatus result, Guid actorId, string? comment)
    {
        if (Status != RequestStatus.Pending)
            throw new InvalidOperationException("The request has already been reviewed.");
        if (result is not (RequestStatus.Approved or RequestStatus.Rejected) || actorId == Guid.Empty)
            throw new ArgumentException("A valid decision and actor are required.");
        if (result == RequestStatus.Rejected && string.IsNullOrWhiteSpace(comment))
            throw new ArgumentException("A rejection reason is required.");

        _history.Add(new RequestStatusHistory(Id, Status, result, actorId, comment?.Trim()));
        Status = result;
    }
}
