using OrganizationRequests.Domain.Enums;
using FastEndpoints;
using OrganizationRequests.Application;

namespace OrganizationRequests.Web.Modules.Auth.Logout;

public sealed class Endpoint : EndpointWithoutRequest<Command.Response>
{
    public override void Configure()
    {
        Post("/api/auth/logout");
        Roles(nameof(UserRole.Employee));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = await new Command().ExecuteAsync(ct);
        await Send.ResponseAsync(response, 200, ct);
    }

    private sealed class EndpointSummary : Summary<Endpoint>
    {
        public EndpointSummary()
        {
            Summary = "Auth: Logout";
            Response<Command.Response>(200, "Successful operation");
            Response<FastEndpoints.ProblemDetails>(400, "Invalid input");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(401, "Authentication required or invalid credentials");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(403, "Insufficient permission");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(404, "Resource not found or not visible");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(409, "Already reviewed or concurrent operation conflict");
        }
    }
}
