namespace BusinessCloud.Application.Bazares.Common;

/// <summary>
/// Normaliza el telefono de un cliente a un formato canonico unico: solo digitos,
/// con el codigo de pais (52 = Mexico por defecto) antepuesto.
/// Se debe usar en TODOS los puntos donde se captura o importa el telefono de un
/// cliente (alta manual, edicion, importacion de clientes, importacion de ventas,
/// fusion de duplicados) para evitar que un mismo numero quede guardado con
/// formatos distintos (con o sin "52") segun el flujo usado, lo cual provoca que
/// el envio de WhatsApp falle o que la deteccion de telefonos duplicados no
/// funcione correctamente.
/// </summary>
public static class PhoneNumberNormalizer
{
    public const string DefaultCountryCode = "52";

    public static string Normalize(string? phone, string defaultCountryCode = DefaultCountryCode)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return string.Empty;

        var cc = new string((defaultCountryCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(cc))
            return digits;

        // Formato antiguo de WhatsApp para moviles de Mexico: codigo de pais + "1" +
        // 10 digitos (13 en total). Meta ya no requiere ese "1" extra, se elimina para
        // dejar el formato actual (codigo de pais + 10 digitos).
        if (digits.Length == cc.Length + 11 && digits.StartsWith(cc + "1"))
            return cc + digits[(cc.Length + 1)..];

        // Numero nacional sin codigo de pais (10 digitos): se antepone el codigo de pais.
        if (digits.Length == 10 && !digits.StartsWith(cc))
            return cc + digits;

        // Ya trae codigo de pais u otro formato: se deja tal cual (solo digitos).
        return digits;
    }
}
