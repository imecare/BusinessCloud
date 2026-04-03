using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Services
{
    public class SaleService : ISaleService
    {
        private readonly PaymentsDbContext _context;
        private readonly IValidator<CreateSaleRequest> _validator;

        public SaleService(PaymentsDbContext context, IValidator<CreateSaleRequest> validator)
        {
            _context = context;
            _validator = validator;
        }

        public async Task<SaleResponse> CreateSaleAsync(CreateSaleRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors); // Esto lo atrapará tu Middleware
            }

            // 1. Validar existencia del cliente
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId);

            if (customer == null)
                throw new KeyNotFoundException($"El cliente con ID {request.CustomerId} no existe.");

            // 2. Lógica de Negocio: Cálculo de Comisión
            // Ejemplo: 10% sobre la utilidad (Precio Venta - Costo Producto)
            decimal profit = request.TotalAmount - request.ProductCost;
            decimal commission = profit > 0 ? profit * 0.10m : 0;

            var sale = new Sale
            {
                CustomerId = request.CustomerId,
                SellerId = request.SellerId,
                TotalAmount = request.TotalAmount,
                ProductCost = request.ProductCost,
                CommissionAmount = commission,
                IsCommissionPaid = false,
                IsPaid = false,
                SaleDate = DateTime.UtcNow
            };

            // 3. Persistencia
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            return new SaleResponse
            {
                Id = sale.Id,
                TotalAmount = sale.TotalAmount,
                RemainingBalance = sale.TotalAmount, // Al ser nueva, el saldo es el total
                IsPaid = false
            };
        }

        public async Task<IEnumerable<SaleResponse>> GetCustomerHistoryAsync(string rfc, string phone)
        {
            // Consulta Senior: Filtramos por RFC y Teléfono para seguridad del cliente
            return await _context.Sales
                .Include(s => s.Payments)
                .Where(s => s.Customer.RFC == rfc && s.Customer.Phone == phone)
                .Select(s => new SaleResponse
                {
                    Id = s.Id,
                    TotalAmount = s.TotalAmount,
                    // Saldo = Total - Suma de abonos
                    RemainingBalance = s.TotalAmount - s.Payments.Sum(p => p.Amount),
                    IsPaid = s.IsPaid
                })
                .ToListAsync();
        }
    }
}
