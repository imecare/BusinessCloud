using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.DeleteLiveSaleDrafts;

public class DeleteLiveSaleDraftsHandler(IBazaresDbContext context)
    : IRequestHandler<DeleteLiveSaleDraftsCommand, int>
{
    public async Task<int> Handle(DeleteLiveSaleDraftsCommand request, CancellationToken ct)
    {
        var query = context.LiveSaleDrafts.Where(x => x.BzaEventId == request.BzaEventId);
        if (request.DraftId is not null) query = query.Where(x => x.Id == request.DraftId);
        var drafts = await query.ToListAsync(ct);
        context.LiveSaleDrafts.RemoveRange(drafts);
        await context.SaveChangesAsync(ct);
        return drafts.Count;
    }
}
