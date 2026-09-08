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

    [Fact]
    public void Create_NormalTransactionIsNotReversal()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            -25m,
            "Groceries",
            DateTimeOffset.UtcNow);

        Assert.False(transaction.IsReversal);
        Assert.Null(transaction.ReversalOfTransactionId);
    }

    [Fact]
    public void CreateReversal_UsesOriginalAccount()
    {
        var original = Transaction.Create(
            Guid.NewGuid(),
            -25m,
            "Groceries",
            DateTimeOffset.UtcNow);

        var reversal = Transaction.CreateReversal(
            original,
            "Entered wrong amount",
            DateTimeOffset.UtcNow);

        Assert.Equal(
            original.AccountId,
            reversal.AccountId);
    }

    [Fact]
    public void CreateReversal_UsesOppositeAmount()
    {
        var original = Transaction.Create(
            Guid.NewGuid(),
            -250m,
            "Groceries",
            DateTimeOffset.UtcNow);

        var reversal = Transaction.CreateReversal(
            original,
            "Entered wrong amount",
            DateTimeOffset.UtcNow);

        Assert.Equal(
            250m,
            reversal.Amount);
    }

    [Fact]
    public void CreateReversal_PositiveOriginalCreatesNegativeReversal()
    {
        var original = Transaction.Create(
            Guid.NewGuid(),
            100m,
            "Deposit",
            DateTimeOffset.UtcNow);

        var reversal = Transaction.CreateReversal(
            original,
            "Deposit entered by mistake",
            DateTimeOffset.UtcNow);

        Assert.Equal(
            -100m,
            reversal.Amount);
    }

    [Fact]
    public void CreateReversal_LinksToOriginalTransaction()
    {
        var original = Transaction.Create(
            Guid.NewGuid(),
            -25m,
            "Groceries",
            DateTimeOffset.UtcNow);

        var reversal = Transaction.CreateReversal(
            original,
            "Entered wrong amount",
            DateTimeOffset.UtcNow);

        Assert.True(reversal.IsReversal);

        Assert.Equal(
            original.Id,
            reversal.ReversalOfTransactionId);
    }

    [Fact]
    public void CreateReversal_TrimsDescription()
    {
        var original = Transaction.Create(
            Guid.NewGuid(),
            -25m,
            "Groceries",
            DateTimeOffset.UtcNow);

        var reversal = Transaction.CreateReversal(
            original,
            "  Entered wrong amount  ",
            DateTimeOffset.UtcNow);

        Assert.Equal(
            "Entered wrong amount",
            reversal.Description);
    }

    [Fact]
    public void CreateReversal_ReversalTransactionFails()
    {
        var original = Transaction.Create(
            Guid.NewGuid(),
            -25m,
            "Groceries",
            DateTimeOffset.UtcNow);

        var reversal = Transaction.CreateReversal(
            original,
            "Entered wrong amount",
            DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            Transaction.CreateReversal(
                reversal,
                "Trying to reverse the reversal",
                DateTimeOffset.UtcNow));
    }
}
