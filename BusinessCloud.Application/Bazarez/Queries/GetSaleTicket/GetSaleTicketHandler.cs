using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetSaleTicket;

/// <summary>
/// Handler para obtener el ticket de un Evento de Venta.
/// Nota: Este handler muestra información global del evento. 
/// Para ticket de un cliente específico, usar GetCustomerEventTicket.
/// </summary>
public class GetSaleTicketHandler(IBazaresDbContext context)
    : IRequestHandler<GetSaleTicketQuery, SaleTicketDto>
{
    private readonly IBazaresDbContext _context = context;

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "Abierto" },
        { 2, "Cerrado" },
        { 3, "En Entrega" },
        { 4, "Finalizado" },
        { 5, "Cancelado" }
    };

    private static readonly Dictionary<int, string> PaymentStatusNames = new()
    {
        { 1, "Preautorizado" },
        { 2, "Aprobado" },
        { 3, "Rechazado" }
    };

    public async Task<SaleTicketDto> Handle(GetSaleTicketQuery request, CancellationToken cancellationToken)
    {
        var saleEvent = await _context.Events
            .Include(s => s.Sales).ThenInclude(x => x.Customer)
            .Include(s => s.Sales).ThenInclude(x => x.Products)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        var totalProducts = saleEvent.Sales.SelectMany(x => x.Products).Sum(p => p.Price);
        var totalPaid = saleEvent.Payments.Where(p => p.IsVerified).Sum(p => p.Amount);
        var uniqueCustomers = saleEvent.Sales.Select(x => x.BzaCustomerId).Distinct().Count();

        return new SaleTicketDto
        {
            SaleId = saleEvent.Id,
            SaleDescription = saleEvent.Description,
            CreatedAt = saleEvent.CreatedAt,
            PaymentDeadline = saleEvent.PaymentDeadline,
            CustomerId = 0, // Evento global, no tiene cliente único
            CustomerName = $"{uniqueCustomers} cliente(s)",
            CustomerPhone = string.Empty,
            CustomerAddress = null,
            Status = saleEvent.Status,
            StatusName = StatusNames.GetValueOrDefault(saleEvent.Status, "Desconocido"),
            Products = saleEvent.Sales.SelectMany(x => x.Products.Select(p => new SaleTicketProductDto
            {
                Id = p.Id,
                Description = $"{x.Customer.Name}: {p.Description}",
                Price = p.Price
            })).ToList(),
            Subtotal = totalProducts,
            TotalPaid = totalPaid,
            PendingAmount = Math.Max(0, totalProducts - totalPaid),
            IsPaid = totalPaid >= totalProducts,
            Payments = saleEvent.Payments.Select(p => new SaleTicketPaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                Date = p.Date,
                PaymentMethod = p.PaymentMethod,
                PaymentStatus = p.PaymentStatus,
                PaymentStatusName = PaymentStatusNames.GetValueOrDefault(p.PaymentStatus, "Desconocido"),
                Reference = p.Reference
            }).OrderByDescending(p => p.Date).ToList(),
            LabelCode = null
        };
    }
}
