using MediatR;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSaleStatus;

public class UpdateBzaSaleStatusHandler : IRequestHandler<UpdateBzaSaleStatusCommand>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public UpdateBzaSaleStatusHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task Handle(UpdateBzaSaleStatusCommand request, CancellationToken cancellationToken)
    {
        // 1. Actualizar SQL
        var sale = await _context.Sales.FindAsync(new object[] { request.SaleId }, cancellationToken);

        if (sale == null) throw new KeyNotFoundException("Venta no encontrada"); ;

        sale.Status = request.NewStatus;
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Auditoría en MongoDB (Histórico de transacciones)
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_PaymentRegistered",
            SaleId = sale.Id,
            NewStatus = request.NewStatus,
            Note = request.Note,
            Timestamp = DateTime.UtcNow,
            TotalConfirmed = sale.Total
        }, cancellationToken);
    }
}