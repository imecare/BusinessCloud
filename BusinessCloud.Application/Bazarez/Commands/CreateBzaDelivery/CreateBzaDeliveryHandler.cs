using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaDelivery;

public class CreateBzaDeliveryHandler : IRequestHandler<CreateBzaDeliveryCommand, int>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public CreateBzaDeliveryHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task<int> Handle(CreateBzaDeliveryCommand request, CancellationToken cancellationToken)
    {
        // Validar que el grupo existe
        var groupExists = await _context.CollectorGroups
            .AnyAsync(g => g.Id == request.BzaCollectorGroupId, cancellationToken);

        if (!groupExists)
            throw new KeyNotFoundException("Grupo de recolectores no encontrado.");

        var delivery = new BzaDelivery
        {
            BzaCollectorGroupId = request.BzaCollectorGroupId,
            DeliveryDate = request.DeliveryDate,
            Status = 1, // Programada
            Notes = request.Notes
        };

        _context.Deliveries.Add(delivery);
        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_DeliveryCreated",
            DeliveryId = delivery.Id,
            GroupId = delivery.BzaCollectorGroupId,
            DeliveryDate = delivery.DeliveryDate,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return delivery.Id;
    }
}
