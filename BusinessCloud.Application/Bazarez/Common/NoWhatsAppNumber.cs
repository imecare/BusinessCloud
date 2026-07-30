using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Common;

/// <summary>
/// Genera y detecta los números placeholder de 10 dígitos que se asignan a los
/// clientes marcados como "sin número de WhatsApp". El número es consecutivo por
/// bazar (TenantId) y monótono: nunca se reutiliza, aunque un cliente se elimine o
/// luego obtenga su teléfono real.
/// </summary>
public static class NoWhatsAppNumber
{
    /// <summary>Longitud fija del número placeholder (10 dígitos).</summary>
    public const int Length = 10;

    /// <summary>
    /// Reserva el siguiente número consecutivo del bazar (ej. "0000000001"). Incrementa
    /// el contador en el contexto; el <c>SaveChangesAsync</c> del caso de uso lo persiste
    /// junto con el resto de la operación. Soporta múltiples reservas dentro del mismo
    /// contexto (importación masiva) resolviendo primero la entidad ya rastreada.
    /// </summary>
    public static async Task<string> ReserveNextAsync(
        IBazaresDbContext context, string tenantId, CancellationToken ct)
    {
        var seq = context.NoWhatsAppSequences.Local
                      .FirstOrDefault(s => s.TenantId == tenantId)
                  ?? await context.NoWhatsAppSequences
                      .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        if (seq is null)
        {
            seq = new BzaNoWhatsAppSequence { TenantId = tenantId, LastNumber = 0 };
            context.NoWhatsAppSequences.Add(seq);
        }

        seq.LastNumber++;
        return seq.LastNumber.ToString($"D{Length}");
    }

    /// <summary>
    /// Indica si un teléfono corresponde al patrón de número placeholder (10 dígitos que
    /// comienzan por cero). Útil para ocultarlo en la interfaz.
    /// </summary>
    public static bool IsPlaceholder(string? phone)
        => !string.IsNullOrEmpty(phone)
           && phone.Length == Length
           && phone[0] == '0'
           && phone.All(char.IsDigit);
}