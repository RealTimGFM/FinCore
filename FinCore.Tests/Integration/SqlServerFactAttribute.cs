namespace FinCore.Tests.Integration;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(
                    "FINCORE_TEST_SQLSERVER")))
        {
            Skip =
                "FINCORE_TEST_SQLSERVER is not configured.";
        }
    }
}