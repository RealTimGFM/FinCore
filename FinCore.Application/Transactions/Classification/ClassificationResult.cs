namespace FinCore.Application.Transactions.Classification;

public sealed record ClassificationResult(
    Guid? CategoryId,
    ClassificationSource Source,
    decimal Confidence)
{
    public static ClassificationResult NoMatch =>
        new(null, ClassificationSource.None, 0m);

    public static ClassificationResult FromMerchantMemory(Guid categoryId)
    {
        return new ClassificationResult(
            categoryId,
            ClassificationSource.MerchantMemory,
            1m);
    }
}
