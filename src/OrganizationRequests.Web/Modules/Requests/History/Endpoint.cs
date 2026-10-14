using FastEndpoints;
using OrganizationRequests.Application;

namespace OrganizationRequests.Web.Modules.Requests.History;

public sealed class Endpoint : Endpoint<Query, Query.Response>
{
    public override void Configure()
    {
        Get("/api/requests/{id}/history");
        Roles("Employee");
    }

    public override async Task HandleAsync(Query req, CancellationToken ct)
    {
        var response = await req.ExecuteAsync(ct);
        await Send.ResponseAsync(response, 200, ct);
    }

    private sealed class EndpointSummary : Summary<Endpoint>
    {
        public EndpointSummary()
        {
            Summary = "Requests: History";
            Response<Query.Response>(200, "Successful operation");
            Response<FastEndpoints.ProblemDetails>(400, "Invalid input");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(401, "Authentication required or invalid credentials");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(403, "Insufficient permission");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(404, "Resource not found or not visible");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(409, "Already reviewed or concurrent operation conflict");
        }
    }
}
