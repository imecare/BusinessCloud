using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace BusinessCloud.Application.Bazares.Commands.UnifyDeliveryDates;
public class UnifyDeliveryDatesHandler(IBazaresDbContext context) : IRequestHandler<UnifyDeliveryDatesCommand, UnifyDeliveryDatesResultDto>
{
 public async Task<UnifyDeliveryDatesResultDto> Handle(UnifyDeliveryDatesCommand request, CancellationToken ct)
 {
  var ids = request.ClosureEventIds.Distinct().ToList(); var updatedGroups = 0;
  await context.ExecuteInTransactionAsync(async transactionCt => { var closures = await context.ClosureEvents.Include(c => c.GroupDeliveries)
   .Where(c => ids.Contains(c.Id) && c.Status != BzaClosureEventStatus.Cancelled && !c.InDeliveryProcess && !c.Delivered).ToListAsync(transactionCt);
   if (closures.Count != ids.Count) throw new InvalidOperationException("Uno o más cierres ya no permiten cambiar su fecha.");
   foreach (var closure in closures) { closure.OfficialDeliveryDate = request.DeliveryDate.Date; foreach (var group in closure.GroupDeliveries) { group.DeliveryDate = request.DeliveryDate.Date; updatedGroups++; } }
   await context.SaveChangesAsync(transactionCt); }, ct);
  return new(ids, request.DeliveryDate.Date, updatedGroups);
 }
}