using MediatR;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSale;

public class CreateBzaSaleHandler : IRequestHandler<CreateBzaSaleCommand, int>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public CreateBzaSaleHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task<int> Handle(CreateBzaSaleCommand request, CancellationToken cancellationToken)
    {
        // 1. Crear la entidad de Venta con sus productos mapeados
        var sale = new BzaSale
        {
            BzaCustomerId = request.BzaCustomerId,
            Description = request.Description,
            Status = 1, // Pendiente de pago inicial
            Total = request.Products.Sum(p => p.Price), // Suma automática
            Products = request.Products.Select(p => new BzaProduct
            {
                Description = p.Description,
                Price = p.Price
            }).ToList()
        };

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Auditoría en MongoDB para el histórico NoSQL
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SaleCreated",
            SaleId = sale.Id,
            Total = sale.Total,
            Timestamp = DateTime.UtcNow,
            Details = $"Venta creada con {sale.Products.Count} productos."
        }, cancellationToken);

        return sale.Id;
    }
}