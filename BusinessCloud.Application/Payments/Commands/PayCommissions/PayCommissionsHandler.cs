using BusinessCloud.Application.Commissions.Commands.PayCommissions;
using BusinessCloud.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class PayCommissionsHandler : IRequestHandler<PayCommissionsCommand, int>
{
    private readonly PaymentsDbContext _context;

    public PayCommissionsHandler(PaymentsDbContext context) => _context = context;

    public async Task<int> Handle(PayCommissionsCommand request, CancellationToken ct)
    {
    // Solo afectamos ventas del mismo Tenant (seguridad SaaS automática) 
        var pendingSales = await _context.Sales
            .Where(s => s.SellerId == request.SellerId
                     && !s.IsCommissionPaid
                     && s.Date <= request.ToDate)
            .ToListAsync(ct);

        foreach (var sale in pendingSales)
        {
            sale.IsCommissionPaid = true; // Marcamos como liquidada 
        }

        return await _context.SaveChangesAsync(ct); // Retorna cuántas comisiones se liquidaron
    }
}