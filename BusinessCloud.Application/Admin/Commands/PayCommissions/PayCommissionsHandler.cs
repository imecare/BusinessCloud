using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.PayCommissions;

public class PayCommissionsHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<PayCommissionsCommand, PayCommissionsResult>
{
    private readonly IIdentityDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<PayCommissionsResult> Handle(
        PayCommissionsCommand request,
        CancellationToken cancellationToken)
    {
        var query = _context.SellerCommissions
            .Where(c => c.SystemSellerId == request.SystemSellerId && !c.IsPaid);

        if (request.CommissionIds is { Count: > 0 })
        {
            var ids = request.CommissionIds.ToHashSet();
            query = query.Where(c => ids.Contains(c.Id));
        }

        var commissions = await query.ToListAsync(cancellationToken);

        if (commissions.Count == 0)
            return new PayCommissionsResult(0, 0m);

        var now = DateTime.UtcNow;
        var total = 0m;

        foreach (var commission in commissions)
        {
            commission.IsPaid = true;
            commission.PaidAt = now;
            commission.PaidBy = _currentUser.UserId;
            if (!string.IsNullOrWhiteSpace(request.Note))
                commission.Notes = request.Note.Trim();
            total += commission.Amount;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new PayCommissionsResult(commissions.Count, total);
    }
}
