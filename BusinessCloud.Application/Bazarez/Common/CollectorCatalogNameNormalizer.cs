using System.Text.RegularExpressions;

namespace BusinessCloud.Application.Bazares.Common;

public static partial class CollectorCatalogNameNormalizer
{
    public const string SpecialName = "..+ ENVIOS";

    public static string Clean(string? value)
    {
        var collapsed = CollapseSpaces(value);
        if (string.Equals(collapsed, SpecialName, StringComparison.Ordinal))
        {
            return SpecialName;
        }

        return OrderPrefixRegex().Replace(collapsed, string.Empty).Trim();
    }

    public static string ToComparisonKey(string? value)
        => Clean(value).ToUpperInvariant();

    public static string CollapseSpaces(string? value)
        => WhiteSpaceRegex().Replace(value?.Trim() ?? string.Empty, " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();

    [GeneratedRegex(@"^\s*\d+(?:\.\d+)*[\s\p{P}\p{S}]*")]
    private static partial Regex OrderPrefixRegex();
}
