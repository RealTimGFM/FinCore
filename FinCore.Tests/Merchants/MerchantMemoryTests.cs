using FinCore.Domain.Merchants;

namespace FinCore.Tests.Merchants;

public sealed class MerchantMemoryTests
{
    [Fact]
    public void Create_StoresMerchantAndCategory()
    {
        var categoryId = Guid.NewGuid();

        var memory = MerchantMemory.Create("COZY HOUSE", categoryId);

        Assert.Equal("COZY HOUSE", memory.NormalizedMerchant);
        Assert.Equal(categoryId, memory.CategoryId);
        Assert.NotEqual(Guid.Empty, memory.Id);
        Assert.Equal(memory.CreatedAtUtc, memory.UpdatedAtUtc);
    }

    [Fact]
    public void RememberCategory_ChangesCategory()
    {
        var originalCategoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var memory = MerchantMemory.Create("COZY HOUSE", originalCategoryId);

        memory.RememberCategory(newCategoryId);

        Assert.Equal(newCategoryId, memory.CategoryId);
    }

    [Fact]
    public void Create_EmptyMerchantIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => MerchantMemory.Create("   ", Guid.NewGuid()));
    }

    [Fact]
    public void Create_EmptyCategoryIdIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => MerchantMemory.Create("COZY HOUSE", Guid.Empty));
    }
}
