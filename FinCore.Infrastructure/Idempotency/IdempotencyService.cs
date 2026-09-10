using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FinCore.Application.Common;
using FinCore.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Infrastructure.Idempotency;

public sealed class IdempotencyService
{
    public const int MaximumKeyLength = 200;

    private readonly FinCoreDbContext _dbContext;

    public IdempotencyService(
        FinCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyResult<TResponse>> ExecuteAsync<
        TRequest,
        TResponse>(
        string idempotencyKey,
        TRequest request,
        Func<Task<TResponse>> operation,
        int successStatusCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency key cannot be empty.",
                nameof(idempotencyKey));
        }

        if (idempotencyKey.Length > MaximumKeyLength)
        {
            throw new ArgumentException(
                $"Idempotency key cannot exceed {MaximumKeyLength} characters.",
                nameof(idempotencyKey));
        }

        ArgumentNullException.ThrowIfNull(operation);

        var requestHash = ComputeRequestHash(request);

        var existing =
            await _dbContext.IdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    record => record.Key == idempotencyKey,
                    cancellationToken);

        if (existing is not null)
        {
            return ReplayExisting<TResponse>(
                existing,
                requestHash);
        }

        await using var dbTransaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var record = new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = idempotencyKey,
            RequestHash = requestHash,
            StatusCode = successStatusCode,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.IdempotencyRecords.Add(record);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            await dbTransaction.RollbackAsync(
                cancellationToken);

            _dbContext.ChangeTracker.Clear();

            var winningRecord =
                await _dbContext.IdempotencyRecords
                    .AsNoTracking()
                    .SingleAsync(
                        item => item.Key == idempotencyKey,
                        cancellationToken);

            return ReplayExisting<TResponse>(
                winningRecord,
                requestHash);
        }

        try
        {
            var response = await operation();

            record.ResponseBody =
                JsonSerializer.Serialize(response);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await dbTransaction.CommitAsync(
                cancellationToken);

            return new IdempotencyResult<TResponse>(
                response,
                successStatusCode,
                WasReplayed: false);
        }
        catch
        {
            await dbTransaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    private static IdempotencyResult<TResponse>
        ReplayExisting<TResponse>(
            IdempotencyRecord record,
            string requestHash)
    {
        if (!string.Equals(
                record.RequestHash,
                requestHash,
                StringComparison.Ordinal))
        {
            throw new IdempotencyKeyReuseException(
                "The idempotency key has already been used for a different request.");
        }

        if (string.IsNullOrWhiteSpace(record.ResponseBody))
        {
            throw new InvalidOperationException(
                "The idempotent operation does not contain a stored response.");
        }

        var response =
            JsonSerializer.Deserialize<TResponse>(
                record.ResponseBody);

        if (response is null)
        {
            throw new InvalidOperationException(
                "Unable to deserialize stored idempotency response.");
        }

        return new IdempotencyResult<TResponse>(
            response,
            record.StatusCode,
            WasReplayed: true);
    }

    private static string ComputeRequestHash<TRequest>(
        TRequest request)
    {
        var json = JsonSerializer.Serialize(request);

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(json));

        return Convert.ToHexString(bytes);
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && sqlException.Number is 2601 or 2627;
    }
}
