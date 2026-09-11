using FinCore.Domain.Categories;

namespace FinCore.Tests.Categories;

public sealed class CategoryTests
{
    [Fact]
    public void Create_TrimsNameAndStartsActive()
    {
        var category = Category.Create("  Restaurants  ");

        Assert.Equal("Restaurants", category.Name);
        Assert.False(category.IsArchived);
        Assert.NotEqual(Guid.Empty, category.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyNameIsRejected(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            Category.Create(name!));
    }

    [Fact]
    public void Create_NameOverOneHundredCharactersIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Category.Create(new string('a', 101)));
    }

    [Fact]
    public void Rename_ArchivedCategoryIsRejected()
    {
        var category = Category.Create("Dining");
        category.Archive();

        Assert.Throws<InvalidOperationException>(() =>
            category.Rename("Restaurants"));
    }

    [Fact]
    public void Archive_AlreadyArchivedCategoryIsRejected()
    {
        var category = Category.Create("Restaurants");
        category.Archive();

        Assert.Throws<InvalidOperationException>(category.Archive);
    }
}
