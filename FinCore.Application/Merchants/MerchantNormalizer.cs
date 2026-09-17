using System.Text;

namespace FinCore.Application.Merchants;

public static class MerchantNormalizer
{
    public static string Normalize(string rawMerchant)
    {
        if (string.IsNullOrWhiteSpace(rawMerchant))
        {
            throw new ArgumentException(
                "Merchant description is required.",
                nameof(rawMerchant));
        }

        var input = rawMerchant
            .Trim()
            .Normalize(NormalizationForm.FormKC)
            .ToUpperInvariant();

        var builder = new StringBuilder(input.Length);
        var previousWasSpace = false;

        foreach (var character in input)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSpace = false;
                continue;
            }

            // Apostrophes should disappear completely:
            // MCDONALD'S -> MCDONALDS
            if (character is '\'' or '’')
            {
                continue;
            }

            // Other punctuation becomes one space.
            if (builder.Length > 0 && !previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder
            .ToString()
            .Trim();
    }
}
