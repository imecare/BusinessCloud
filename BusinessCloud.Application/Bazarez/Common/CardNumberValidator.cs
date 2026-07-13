using System.Text.RegularExpressions;

namespace BusinessCloud.Application.Bazares.Common;

/// <summary>
/// Validación local de números de tarjeta (sin servicios externos).
/// </summary>
public static partial class CardNumberValidator
{
    [GeneratedRegex(@"[\s-]")]
    private static partial Regex SeparatorsRegex();

    /// <summary>
    /// Indica si la cadena representa un número de tarjeta con formato válido:
    /// solo dígitos (admite espacios/guiones), entre 13 y 19 dígitos y que pase Luhn.
    /// </summary>
    public static bool IsValid(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return false;
        }

        var digits = SeparatorsRegex().Replace(cardNumber, "");

        if (digits.Length < 13 || digits.Length > 19)
        {
            return false;
        }

        foreach (var c in digits)
        {
            if (c < '0' || c > '9')
            {
                return false;
            }
        }

        return PassesLuhn(digits);
    }

    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var doubleDigit = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var value = digits[i] - '0';
            if (doubleDigit)
            {
                value *= 2;
                if (value > 9)
                {
                    value -= 9;
                }
            }
            sum += value;
            doubleDigit = !doubleDigit;
        }
        return sum % 10 == 0;
    }
}
