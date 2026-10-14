using OrganizationRequests.Domain.Enums;
using FastEndpoints;
using OrganizationRequests.Application;

namespace OrganizationRequests.Web.Modules.Requests.Reject;

public sealed class Endpoint : Endpoint<Command, RequestResponse>
{
    public override void Configure()
    {
        Post("/api/requests/{id}/reject");
        Roles(nameof(UserRole.Reviewer));
    }

    public override async Task HandleAsync(Command req, CancellationToken ct)
    {
        var response = await req.ExecuteAsync(ct);
        await Send.ResponseAsync(response, 200, ct);
    }

    private sealed class EndpointSummary : Summary<Endpoint>
    {
        public EndpointSummary()
        {
            Summary = "Requests: Reject";
            Response<RequestResponse>(200, "Successful operation");
            Response<FastEndpoints.ProblemDetails>(400, "Invalid input");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(401, "Authentication required or invalid credentials");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(403, "Insufficient permission");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(404, "Resource not found or not visible");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(409, "Already reviewed or concurrent operation conflict");
        }
    }
}
