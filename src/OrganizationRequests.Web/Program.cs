using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using FastEndpoints;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using OrganizationRequests.Application;
using OrganizationRequests.Infrastructure;
using OrganizationRequests.Infrastructure.Persistence;
using OrganizationRequests.Infrastructure.Security;
using OrganizationRequests.Web.Security;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("SqlServer");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:SqlServer using user-secrets or environment variables.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddFastEndpoints();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "__Host-organization-requests";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            var services = context.HttpContext.RequestServices;
            var users = services.GetRequiredService<UserManager<Account>>();
            var signIn = services.GetRequiredService<SignInManager<Account>>();
            var account = await signIn.ValidateSecurityStampAsync(context.Principal);
            if (account is null || !account.IsActive || await users.IsLockedOutAsync(account))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return;
            }
            context.ReplacePrincipal(await signIn.CreateUserPrincipalAsync(account));
            context.ShouldRenew = false;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-organization-requests-csrf";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var protection = builder.Services.AddDataProtection()
    .SetApplicationName("OrganizationRequests")
    .PersistKeysToDbContext<AppDbContext>();
var certificatePath = builder.Configuration["DataProtection:CertificatePath"];
if (!string.IsNullOrWhiteSpace(certificatePath))
    protection.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(
        certificatePath, builder.Configuration["DataProtection:CertificatePassword"]));
else if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Configure a shared DataProtection:CertificatePath in production.");

var app = builder.Build();
if (args.Contains("--seed"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Demo seed is allowed only in Development.");
    var password = builder.Configuration["Seed:Password"]
        ?? throw new InvalidOperationException("Configure Seed:Password in user-secrets before seeding.");
    await app.Services.SeedDevelopmentAsync(password, CancellationToken.None);
    app.Logger.LogInformation("Development users employee/reviewer and IT destination unit are ready.");
    return;
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method))
    {
        // Preserve 401 for unauthenticated protected writes; login still requires CSRF.
        if (context.User.Identity?.IsAuthenticated != true && context.Request.Path != "/api/auth/login")
            throw new RequestException(401, "Authentication is required.");
        await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
    }
    await next(context);
});
app.UseFastEndpoints(config =>
{
    config.Serializer.Options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    config.Security.RoleClaimType = ClaimTypes.Role;
    config.Errors.UseProblemDetails();
});
app.Run();
