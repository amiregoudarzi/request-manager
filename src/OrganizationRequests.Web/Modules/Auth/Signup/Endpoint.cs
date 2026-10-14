using FastEndpoints;

namespace OrganizationRequests.Web.Modules.Auth.Signup;

public sealed class Endpoint : Endpoint<Command, Command.Response>
{
    public override void Configure()
    {
        Post("/api/auth/signup");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Command req, CancellationToken ct)
    {
        var response = await req.ExecuteAsync(ct);
        await Send.ResponseAsync(response, 201, ct);
    }

    private sealed class EndpointSummary : Summary<Endpoint>
    {
        public EndpointSummary()
        {
            Summary = "Register an employee";
            Description = "Creates an employee account using username, password and display name. Passwords require at least 12 characters, uppercase, lowercase, a digit and a non-alphanumeric character. Reviewer permissions cannot be assigned through signup. Login separately after registration. Requires CSRF protection; Swagger handles it automatically.";
            Response<Command.Response>(201, "Employee account created");
            Response<FastEndpoints.ProblemDetails>(400, "Invalid signup input");
            Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(409, "Username already registered");
        }
    }
}
