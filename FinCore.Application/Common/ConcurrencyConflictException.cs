namespace FinCore.Application.Common;

public sealed class ConcurrencyConflictException
    : Exception
{
    public ConcurrencyConflictException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}