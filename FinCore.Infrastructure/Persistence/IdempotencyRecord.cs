namespace FinCore.Infrastructure.Persistence;

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }

    public required string Key { get; set; }

    public required string RequestHash { get; set; }

    public int StatusCode { get; set; }

    public string? ResponseBody { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
