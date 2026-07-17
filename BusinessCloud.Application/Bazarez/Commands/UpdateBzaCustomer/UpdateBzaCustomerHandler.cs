using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Common;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaCustomer;

public class UpdateBzaCustomerHandler : IRequestHandler<UpdateBzaCustomerCommand>
{
    private readonly IBazaresDbContext _context;

    public UpdateBzaCustomerHandler(IBazaresDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateBzaCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Customers
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Cliente de bazar con ID {request.Id} no encontrado.");
        }

        var collectorExists = await _context.Collectors
            .AnyAsync(c => c.Id == request.BzaCollectorId, cancellationToken);

        if (!collectorExists)
        {
            throw new Exception($"El recolector con ID {request.BzaCollectorId} no existe.");
        }

        // El teléfono es la llave para el envío de totales: se normaliza y debe ser único entre clientes.
        var phone = NormalizePhone(request.Phone);
        var facebookName = FacebookMessengerProfile.Normalize(request.FacebookName);

        var duplicate = await _context.Customers
            .AnyAsync(c => c.Phone == phone && c.Id != request.Id, cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"Ya existe otro cliente registrado con el teléfono {phone}. El teléfono debe ser único.");
        }

        entity.Name = request.Name;
        entity.FacebookName = facebookName;
        entity.Phone = phone;
        entity.Status = request.Status;
        entity.BzaCollectorId = request.BzaCollectorId;

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Deja solo los dígitos del teléfono para usarlo como llave única.</summary>
    private static string NormalizePhone(string? phone)
        => new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
}