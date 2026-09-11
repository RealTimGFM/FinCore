namespace FinCore.Application.Categories;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc);
