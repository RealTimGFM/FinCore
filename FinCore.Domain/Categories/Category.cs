namespace FinCore.Domain.Categories;

public sealed class Category
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public bool IsArchived { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Required by EF Core.
    private Category()
    {
    }

    private Category(
        Guid id,
        string name,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = createdAtUtc;
        IsArchived = false;
    }

    public static Category Create(string name)
    {
        name = ValidateName(name);

        return new Category(
            Guid.NewGuid(),
            name,
            DateTimeOffset.UtcNow);
    }

    public void Rename(string newName)
    {
        if (IsArchived)
        {
            throw new InvalidOperationException(
                "An archived category cannot be renamed.");
        }

        Name = ValidateName(newName);
    }

    public void Archive()
    {
        if (IsArchived)
        {
            throw new InvalidOperationException(
                "The category is already archived.");
        }

        IsArchived = true;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Category name is required.",
                nameof(name));
        }

        name = name.Trim();

        if (name.Length > 100)
        {
            throw new ArgumentException(
                "Category name cannot exceed 100 characters.",
                nameof(name));
        }

        return name;
    }
}
