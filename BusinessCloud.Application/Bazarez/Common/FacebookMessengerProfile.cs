using System.Text.RegularExpressions;

namespace BusinessCloud.Application.Bazares.Common;

public static partial class FacebookMessengerProfile
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var input = value.Trim();
        string candidate = input;

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.ToLowerInvariant();

            if (host == "m.me")
            {
                candidate = FirstSegment(uri.AbsolutePath);
            }
            else if (host.Contains("messenger.com") && uri.AbsolutePath.StartsWith("/t/", StringComparison.OrdinalIgnoreCase))
            {
                candidate = FirstSegment(uri.AbsolutePath[3..]);
            }
            else if (host.Contains("facebook.com"))
            {
                if (uri.AbsolutePath.Contains("profile.php", StringComparison.OrdinalIgnoreCase))
                {
                    candidate = GetQueryValue(uri.Query, "id") ?? string.Empty;
                }
                else
                {
                    candidate = FirstSegment(uri.AbsolutePath);
                }
            }
        }

        candidate = Uri.UnescapeDataString(candidate)
            .Trim()
            .Trim('/')
            .TrimStart('@');

        return string.IsNullOrWhiteSpace(candidate) ? null : candidate;
    }

    public static bool IsValid(string? value)
    {
        var normalized = Normalize(value);
        return normalized is not null && FacebookProfileRegex().IsMatch(normalized);
    }

    public static string? NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var input = value.Trim();
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return null;

        if (!IsSupportedFacebookHost(uri.Host))
            return null;

        if ((uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || string.IsNullOrWhiteSpace(uri.Host))
            return null;

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + uri.Query;
    }

    public static bool IsValidUrl(string? value) => NormalizeUrl(value) is not null;

    private static string FirstSegment(string path)
    {
        var trimmed = (path ?? string.Empty).Trim('/');
        var slashIndex = trimmed.IndexOf('/');
        return slashIndex >= 0 ? trimmed[..slashIndex] : trimmed;
    }

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        var parts = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var tokens = part.Split('=', 2);
            if (tokens.Length == 2 && tokens[0].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return tokens[1];
            }
        }

        return null;
    }

    private static bool IsSupportedFacebookHost(string? host)
    {
        var value = (host ?? string.Empty).ToLowerInvariant();
        return value == "m.me"
            || value == "fb.com"
            || value.EndsWith("facebook.com", StringComparison.Ordinal)
            || value.EndsWith("messenger.com", StringComparison.Ordinal);
    }

    [GeneratedRegex("^(?:\\d{5,20}|(?![.])(?!.*[.]{2})(?!.*[.]$)[A-Za-z0-9._-]{3,100})$")]
    private static partial Regex FacebookProfileRegex();
}