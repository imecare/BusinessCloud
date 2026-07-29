namespace BusinessCloud.Application.Common.Utilities;

/// <summary>
/// Genera los posibles formatos de un numero telefonico mexicano para cruzarlo contra
/// los telefonos almacenados (que pueden guardarse a 10 digitos, con lada 52, o con el
/// "1" extra de movil que agrega WhatsApp: 521 + 10 digitos).
/// </summary>
public static class PhoneNumberCandidates
{
    public static IReadOnlyList<string> Build(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
            return Array.Empty<string>();

        var set = new HashSet<string>(StringComparer.Ordinal) { digits };

        // Determinar el nucleo nacional de 10 digitos (Mexico).
        string? core = null;
        if (digits.Length == 10)
        {
            core = digits;
        }
        else if (digits.StartsWith("52", StringComparison.Ordinal))
        {
            var rest = digits[2..];
            if (rest.Length == 11 && rest[0] == '1')      // 52 + 1 + 10 (movil MX que envia WhatsApp)
                core = rest[1..];
            else if (rest.Length == 10)                   // 52 + 10
                core = rest;
        }
        else if (digits.Length == 11 && digits[0] == '1') // 1 + 10
        {
            core = digits[1..];
        }

        if (core is { Length: 10 })
        {
            set.Add(core);
            set.Add("52" + core);
            set.Add("521" + core);
        }

        return set.OrderByDescending(x => x.Length).ToList();
    }
}