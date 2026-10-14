using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrganizationRequests.Application;
using OrganizationRequests.Application.Abstractions;

namespace OrganizationRequests.Infrastructure.Persistence;

public sealed class SqlDistributedLock(AppDbContext db) : IDistributedLock
{
    public async Task AcquireAsync(string resource, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        if (resource.Length > 255) throw new ArgumentException("Lock resource is too long.");
        var transaction = db.Database.CurrentTransaction
            ?? throw new InvalidOperationException("A transaction is required before acquiring a lock.");

        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandTimeout = 15;
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 5000;
            SELECT @result;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.DbType = DbType.String;
        parameter.Size = 255;
        parameter.Value = resource;
        command.Parameters.Add(parameter);
        var result = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        if (result == -2) throw new OperationCanceledException("Lock acquisition was cancelled.", ct);
        if (result is -1 or -3)
            throw new RequestException(409, "The request is being processed. Please try again.");
        if (result < 0) throw new InvalidOperationException($"Database lock failed ({result}).");
    }
}
