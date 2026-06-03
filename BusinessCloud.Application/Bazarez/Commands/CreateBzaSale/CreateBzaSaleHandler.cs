using MediatR;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSale;

public class CreateBzaSaleHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<CreateBzaSaleCommand, int>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<int> Handle(CreateBzaSaleCommand request, CancellationToken cancellationToken)
    {
        // 1. Crear el Evento de Venta (Corte/Catálogo/En Vivo)
        var saleEvent = new BzaSale
        {
            Description = request.Description,
            PaymentDeadline = request.PaymentDeadline,
            DeliveryDate = request.DeliveryDate,
            Status = 1 // 1=Abierto (Activo)
        };

        _context.Sales.Add(saleEvent);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Auditoría en MongoDB para el histórico NoSQL
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SaleEventCreated",
            SaleId = saleEvent.Id,
            Description = saleEvent.Description,
            PaymentDeadline = saleEvent.PaymentDeadline,
            DeliveryDate = saleEvent.DeliveryDate,
            Timestamp = DateTime.UtcNow,
            Details = "Evento de Venta creado."
        }, cancellationToken);

        return saleEvent.Id;
    }
}