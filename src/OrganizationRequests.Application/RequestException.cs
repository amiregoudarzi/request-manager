namespace OrganizationRequests.Application;

public sealed class RequestException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
