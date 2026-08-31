using FinCore.Domain.Accounts;

namespace FinCore.Tests.Accounts;

public sealed class AccountTests
{
    [Fact]
    public void Create_StartsActive()
    {
        var account = CreateAccount();

        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void Create_ActiveAccountHasNoClosedAtUtc()
    {
        var account = CreateAccount();

        Assert.Null(account.ClosedAtUtc);
    }

    [Fact]
    public void Create_NormalizesCurrency()
    {
        var account = Account.Create(
            "TD Chequing",
            AccountType.Chequing,
            "  cad  ");

        Assert.Equal("CAD", account.Currency);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("ABC")]
    public void Create_RejectsUnsupportedCurrency(string currency)
    {
        Assert.Throws<ArgumentException>(() => Account.Create(
            "TD Chequing",
            AccountType.Chequing,
            currency));
    }

    [Fact]
    public void Close_ActiveAccountChangesStatusToClosed()
    {
        var account = CreateAccount();

        account.Close("Bank account was closed.", AccountStatusChangeSource.User);

        Assert.Equal(AccountStatus.Closed, account.Status);
    }

    [Fact]
    public void Close_SetsClosedAtUtc()
    {
        var before = DateTimeOffset.UtcNow;
        var account = CreateAccount();

        account.Close("Bank account was closed.", AccountStatusChangeSource.User);

        Assert.NotNull(account.ClosedAtUtc);
        Assert.InRange(account.ClosedAtUtc.Value, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Close_RecordsStatusHistoryEntry()
    {
        var account = CreateAccount();

        account.Close("Bank account was closed.", AccountStatusChangeSource.User);

        Assert.Single(account.StatusChanges);
    }

    [Fact]
    public void Close_HistoryContainsTransitionDetails()
    {
        var before = DateTimeOffset.UtcNow;
        var account = CreateAccount();

        account.Close("  Bank account was closed.  ", AccountStatusChangeSource.User);

        var change = Assert.Single(account.StatusChanges);
        Assert.Equal(AccountStatus.Active, change.FromStatus);
        Assert.Equal(AccountStatus.Closed, change.ToStatus);
        Assert.Equal("Bank account was closed.", change.Reason);
        Assert.Equal(AccountStatusChangeSource.User, change.Source);
        Assert.InRange(change.ChangedAtUtc, before, DateTimeOffset.UtcNow);
        Assert.Equal(account.ClosedAtUtc, change.ChangedAtUtc);
    }

    [Fact]
    public void Close_ClosedAccountFails()
    {
        var account = CreateAccount();
        account.Close("Bank account was closed.", AccountStatusChangeSource.User);

        Assert.Throws<InvalidOperationException>(() => account.Close(
            "Trying again.",
            AccountStatusChangeSource.User));
    }

    [Fact]
    public void Reopen_ClosedAccountChangesStatusToActive()
    {
        var account = CreateClosedAccount();

        account.Reopen("Closed by mistake.", AccountStatusChangeSource.User);

        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void Reopen_ClearsClosedAtUtc()
    {
        var account = CreateClosedAccount();

        account.Reopen("Closed by mistake.", AccountStatusChangeSource.User);

        Assert.Null(account.ClosedAtUtc);
    }

    [Fact]
    public void Reopen_PreservesCloseHistoryEntry()
    {
        var account = CreateClosedAccount();
        var closeChange = Assert.Single(account.StatusChanges);

        account.Reopen("Closed by mistake.", AccountStatusChangeSource.User);

        Assert.Equal(2, account.StatusChanges.Count);
        Assert.Contains(closeChange, account.StatusChanges);
    }

    [Fact]
    public void Reopen_RecordsClosedToActiveHistoryEntry()
    {
        var account = CreateClosedAccount();

        account.Reopen("Closed by mistake.", AccountStatusChangeSource.System);

        var change = account.StatusChanges.Last();
        Assert.Equal(AccountStatus.Closed, change.FromStatus);
        Assert.Equal(AccountStatus.Active, change.ToStatus);
        Assert.Equal("Closed by mistake.", change.Reason);
        Assert.Equal(AccountStatusChangeSource.System, change.Source);
    }

    [Fact]
    public void Reopen_ActiveAccountFails()
    {
        var account = CreateAccount();

        Assert.Throws<InvalidOperationException>(() => account.Reopen(
            "Trying to reopen an active account.",
            AccountStatusChangeSource.User));
    }

    [Fact]
    public void Rename_ClosedAccountChangesName()
    {
        var account = CreateClosedAccount();

        account.Rename("TD Everyday Chequing");

        Assert.Equal("TD Everyday Chequing", account.Name);
        Assert.Equal(AccountStatus.Closed, account.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Close_EmptyReasonIsRejected(string? reason)
    {
        var account = CreateAccount();

        Assert.Throws<ArgumentException>(() => account.Close(
            reason!,
            AccountStatusChangeSource.User));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reopen_EmptyReasonIsRejected(string? reason)
    {
        var account = CreateClosedAccount();

        Assert.Throws<ArgumentException>(() => account.Reopen(
            reason!,
            AccountStatusChangeSource.User));
    }

    private static Account CreateAccount()
    {
        return Account.Create(
            "TD Chequing",
            AccountType.Chequing,
            "CAD");
    }

    private static Account CreateClosedAccount()
    {
        var account = CreateAccount();
        account.Close(
            "Bank account was closed.",
            AccountStatusChangeSource.User);

        return account;
    }
}
