using FinCore.Application.Merchants;

namespace FinCore.Tests.Merchants;

public sealed class MerchantNormalizerTests
{
    [Fact]
    public void Normalize_TrimsAndUppercases()
    {
        var result =
            MerchantNormalizer.Normalize(
                "  Cozy House  ");

        Assert.Equal(
            "COZY HOUSE",
            result);
    }

    [Fact]
    public void Normalize_HyphenBecomesSpace()
    {
        var result =
            MerchantNormalizer.Normalize(
                "cozy-house");

        Assert.Equal(
            "COZY HOUSE",
            result);
    }

    [Fact]
    public void Normalize_CollapsesRepeatedPunctuationAndSpaces()
    {
        var result =
            MerchantNormalizer.Normalize(
                "  COZY---HOUSE   # 123  ");

        Assert.Equal(
            "COZY HOUSE 123",
            result);
    }

    [Theory]
    [InlineData("McDonald's")]
    [InlineData("McDonald’s")]
    public void Normalize_RemovesApostrophes(string description)
    {
        var result = MerchantNormalizer.Normalize(description);

        Assert.Equal("MCDONALDS", result);
    }

    [Fact]
    public void Normalize_PreservesNumbers()
    {
        var result =
            MerchantNormalizer.Normalize(
                "7-Eleven #1234");

        Assert.Equal(
            "7 ELEVEN 1234",
            result);
    }

    [Fact]
    public void Normalize_DoesNotResolvePaymentPrefixesOrStoreNumbers()
    {
        var result = MerchantNormalizer.Normalize("SQ *MCDONALDS #1234");

        Assert.Equal("SQ MCDONALDS 1234", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_EmptyDescriptionIsRejected(
        string? description)
    {
        Assert.Throws<ArgumentException>(
            () => MerchantNormalizer.Normalize(
                description!));
    }
}
