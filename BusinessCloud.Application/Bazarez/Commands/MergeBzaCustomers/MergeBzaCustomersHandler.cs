using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.MergeBzaCustomers;

public class MergeBzaCustomersHandler : IRequestHandler<MergeBzaCustomersCommand, MergeBzaCustomersResultDto>
{
    private readonly IBazaresDbContext _context;

    public MergeBzaCustomersHandler(IBazaresDbContext context) => _context = context;

    public async Task<MergeBzaCustomersResultDto> Handle(MergeBzaCustomersCommand request, CancellationToken ct)
    {
        var mergeIds = (request.MergeIds ?? []).Distinct().Where(id => id != request.SurvivorId).ToList();
        if (mergeIds.Count == 0)
        {
            throw new InvalidOperationException("Debes seleccionar al menos un cliente duplicado para unir.");
        }

        // Cliente que se conserva.
        var survivor = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.SurvivorId, ct)
            ?? throw new InvalidOperationException($"El cliente con ID {request.SurvivorId} no existe.");

        // Clientes duplicados a fusionar.
        var duplicates = await _context.Customers
            .Where(c => mergeIds.Contains(c.Id))
            .ToListAsync(ct);

        if (duplicates.Count != mergeIds.Count)
        {
            throw new InvalidOperationException("Uno o más clientes seleccionados ya no existen.");
        }

        // El recolector elegido debe existir.
        var collectorExists = await _context.Collectors
            .AnyAsync(c => c.Id == request.BzaCollectorId, ct);
        if (!collectorExists)
        {
            throw new InvalidOperationException($"El recolector con ID {request.BzaCollectorId} no existe.");
        }

        // El teléfono es la llave de envío de totales: debe quedar único.
        var phone = NormalizePhone(request.Phone);
        var allIds = mergeIds.Append(request.SurvivorId).ToList();
        var phoneTakenByOther = await _context.Customers
            .AnyAsync(c => c.Phone == phone && !allIds.Contains(c.Id), ct);
        if (phoneTakenByOther)
        {
            throw new InvalidOperationException(
                $"El teléfono {phone} ya pertenece a otro cliente. Elige un teléfono distinto.");
        }

        // ── 1) Reasignar todo el historial de los duplicados al cliente conservado ──
        var salesToMove = await _context.Sales
            .Where(s => mergeIds.Contains(s.BzaCustomerId))
            .ToListAsync(ct);
        foreach (var sale in salesToMove)
        {
            sale.BzaCustomerId = survivor.Id;
        }

        var paymentsToMove = await _context.Payments
            .Where(p => mergeIds.Contains(p.BzaCustomerId))
            .ToListAsync(ct);
        foreach (var payment in paymentsToMove)
        {
            payment.BzaCustomerId = survivor.Id;
        }

        var totalsToMove = await _context.ClosureCustomerTotals
            .Where(t => mergeIds.Contains(t.BzaCustomerId))
            .ToListAsync(ct);
        foreach (var total in totalsToMove)
        {
            total.BzaCustomerId = survivor.Id;
        }

        await _context.SaveChangesAsync(ct);

        // ── 2) Eliminar los clientes duplicados (ya sin referencias) ──
        // Se elimina antes de actualizar el teléfono del conservado para liberar
        // el índice único (TenantId, Phone) si el nuevo teléfono provenía de un duplicado.
        _context.Customers.RemoveRange(duplicates);
        await _context.SaveChangesAsync(ct);

        // ── 3) Aplicar los datos elegidos al cliente conservado ──
        survivor.Name = request.Name.Trim();
        survivor.Phone = phone;
        survivor.FacebookName = string.IsNullOrWhiteSpace(request.FacebookName) ? null : request.FacebookName.Trim();
        survivor.Status = request.Status;
        survivor.BzaCollectorId = request.BzaCollectorId;
        await _context.SaveChangesAsync(ct);

        return new MergeBzaCustomersResultDto(
            survivor.Id,
            duplicates.Count,
            salesToMove.Count,
            paymentsToMove.Count,
            totalsToMove.Count);
    }

    /// <summary>Deja solo los dígitos del teléfono para usarlo como llave única.</summary>
    private static string NormalizePhone(string? phone)
        => new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
}
