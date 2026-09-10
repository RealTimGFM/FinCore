namespace FinCore.Application.Common;

public sealed class IdempotencyKeyReuseException
    : Exception
{
    public IdempotencyKeyReuseException(string message)
        : base(message)
    {
    }
}
