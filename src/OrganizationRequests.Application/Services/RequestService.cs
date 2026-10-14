using OrganizationRequests.Application.Abstractions;
using OrganizationRequests.Domain.Entities;

namespace OrganizationRequests.Application.Services;

public sealed class RequestService(IRequestRepository repository) : IRequestService
{
    public Task<RequestResponse> CreateAsync(string title, string description, RequestType type,
        Guid creatorId, Guid destinationUnitId, Guid? receiverEmployeeId, CancellationToken ct) =>
        repository.ExecuteInTransactionAsync(async token =>
        {
            var context = await repository.GetCreationContextAsync(creatorId, destinationUnitId, receiverEmployeeId, token);
            if (context is null)
                throw new RequestException(403, "Your account must be assigned to an organizational unit first.");
            if (!context.HasActiveUnit)
                throw new RequestException(404, "Destination unit was not found or is inactive.");
            if (!context.HasReceiver)
                throw new RequestException(400, "Receiver must be an employee of the destination unit.");

            var request = new ServiceRequest(title, description, type, creatorId, destinationUnitId, receiverEmployeeId);
            repository.Add(request);
            return RequestResponse.From(request);
        }, ct);

    public Task<RequestResponse> ReviewAsync(Guid requestId, Guid actorId,
        RequestStatus result, string? comment, CancellationToken ct) =>
        repository.ExecuteInTransactionAsync(async token =>
        {
            var request = await repository.GetForReviewAsync(requestId, token)
                ?? throw new RequestException(404, "Request was not found.");
            if (!await repository.HasReviewerAsync(actorId, request.OrganizationalUnitId, token))
                throw new RequestException(403, "You cannot review requests for this destination unit.");
            if (request.Status != RequestStatus.Pending)
                throw new RequestException(409, "The request has already been reviewed.");
            if (result is not (RequestStatus.Approved or RequestStatus.Rejected))
                throw new RequestException(400, "A valid review decision is required.");
            if (comment?.Length > 1000 || (result == RequestStatus.Rejected && string.IsNullOrWhiteSpace(comment)))
                throw new RequestException(400, "A rejection reason is required and decision comments cannot exceed 1000 characters.");

            request.Review(result, actorId, comment);
            return RequestResponse.From(request);
        }, ct);
}
