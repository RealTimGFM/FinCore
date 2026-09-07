using FinCore.Domain.Transactions;

namespace FinCore.Tests.Transactions;

public sealed class TransactionTests
{
    [Fact]
    public void Create_PositiveAmountIsAllowed()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            100m,
            "Salary",
            DateTimeOffset.UtcNow);

        Assert.Equal(100m, transaction.Amount);
    }

    [Fact]
    public void Create_NegativeAmountIsAllowed()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            -25.50m,
            "Groceries",
            DateTimeOffset.UtcNow);

        Assert.Equal(-25.50m, transaction.Amount);
    }

    [Fact]
    public void Create_ZeroAmountIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Transaction.Create(
                Guid.NewGuid(),
                0m,
                "Nothing",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_EmptyAccountIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Transaction.Create(
                Guid.Empty,
                -10m,
                "Coffee",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_TrimsDescription()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            -10m,
            "  Coffee  ",
            DateTimeOffset.UtcNow);

        Assert.Equal(
            "Coffee",
            transaction.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyDescriptionIsRejected(
        string? description)
    {
        Assert.Throws<ArgumentException>(() =>
            Transaction.Create(
                Guid.NewGuid(),
                -10m,
                description!,
                DateTimeOffset.UtcNow));
    }
}