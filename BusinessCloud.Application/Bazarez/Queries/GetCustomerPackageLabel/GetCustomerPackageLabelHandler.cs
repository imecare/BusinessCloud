using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerPackageLabel;

public class GetCustomerPackageLabelHandler(IBazaresDbContext context)
    : IRequestHandler<GetCustomerPackageLabelQuery, CustomerPackageLabelDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<CustomerPackageLabelDto> Handle(GetCustomerPackageLabelQuery request, CancellationToken cancellationToken)
    {
        // 1. Validar que el cliente exista con sus relaciones
        var customer = await _context.Customers
            .Include(c => c.Collector)
                .ThenInclude(col => col.CollectorGroup)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // 2. Validar que el evento de venta exista
        var saleEvent = await _context.Events
            .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        // 3. Obtener productos vendidos al cliente en este evento
        var products = await _context.SoldProducts
            .Where(p => p.Sale.BzaEventId == request.SaleId && p.Sale.BzaCustomerId == request.CustomerId)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
            throw new InvalidOperationException("El cliente no tiene productos vendidos en este evento de venta.");

        // 4. Calcular totales
        var totalAmount = products.Sum(p => p.Price);
        var totalPaid = await _context.Payments
            .Where(p => p.BzaEventId == request.SaleId && p.BzaCustomerId == request.CustomerId && p.IsVerified)
            .SumAsync(p => p.Amount, cancellationToken);

        var isPaid = totalPaid >= totalAmount;

        // 5. Generar código de etiqueta único
        var labelCode = $"BZA-{saleEvent.Id:D4}-{customer.Id:D5}";
        var qrData = $"bza://label/{saleEvent.Id}/{customer.Id}/{labelCode}";

        return new CustomerPackageLabelDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            CustomerAddress = customer.Address,
            SaleEventId = saleEvent.Id,
            EventDescription = saleEvent.Description,
            CollectorName = customer.Collector.Name,
            CollectorGroupName = customer.Collector.CollectorGroup?.Description ?? string.Empty,
            ProductsCount = products.Count,
            TotalAmount = totalAmount,
            IsPaid = isPaid,
            LabelCode = labelCode,
            QrData = qrData
        };
    }
}
