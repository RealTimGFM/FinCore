namespace FinCore.Application.Merchants;

public sealed record MerchantMemoryDto(
    Guid Id,
    string NormalizedMerchant,
    Guid CategoryId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
