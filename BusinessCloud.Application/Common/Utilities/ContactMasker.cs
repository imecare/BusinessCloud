namespace BusinessCloud.Application.Common.Utilities;

public static class ContactMasker
{
    public static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static string NormalizePhoneDigits(string? phone)
        => new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

    public static string MaskEmail(string? email)
    {
        var normalized = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 0)
            return normalized.Length <= 3 ? normalized : normalized[..3] + "***";

        var local = normalized[..atIndex];
        var domain = normalized[(atIndex + 1)..];
        var prefix = local.Length <= 3 ? local : local[..3];
        return prefix + "***@" + domain;
    }

    public static string MaskPhone(string? phone)
    {
        var digits = NormalizePhoneDigits(phone);
        if (string.IsNullOrWhiteSpace(digits))
            return string.Empty;

        if (digits.Length <= 3)
            return new string('*', digits.Length);

        return new string('*', digits.Length - 3) + digits[^3..];
    }
}
