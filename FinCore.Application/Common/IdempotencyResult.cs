namespace FinCore.Application.Common;

public sealed record IdempotencyResult<T>(
    T Value,
    int StatusCode,
    bool WasReplayed);
