using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace BusinessCloud.Application.Bazares.Commands.ProcessDeliveryBatch;
public class ProcessDeliveryBatchHandler(IBazaresDbContext context) : IRequestHandler<ProcessDeliveryBatchCommand, ProcessDeliveryBatchResultDto>
{
 public async Task<ProcessDeliveryBatchResultDto> Handle(ProcessDeliveryBatchCommand request, CancellationToken ct)
 {
  var ids = request.ClosureEventIds.Distinct().ToList(); var affected = 0; int? targetId = null; var deliveryBatchId = Guid.NewGuid();
  await context.ExecuteInTransactionAsync(async transactionCt =>
  {
   var closures = await context.ClosureEvents.Include(c => c.Items).Include(c => c.GroupDeliveries)
    .Where(c => ids.Contains(c.Id)
     && c.Status != BzaClosureEventStatus.Cancelled
     && !c.InDeliveryProcess
     && !c.Delivered
     && c.CustomerTotals.Any(t => t.Status == BzaClosureCustomerTotalStatus.Validated))
    .ToListAsync(transactionCt);
   if (closures.Count != ids.Count) throw new InvalidOperationException("Uno o más cierres ya no pueden iniciar entrega.");
   var totals = await context.ClosureCustomerTotals.Include(t => t.Customer).Include(t => t.Proofs).Include(t => t.ClosureEvent)
    .Where(t => ids.Contains(t.BzaClosureEventId) && t.Status == BzaClosureCustomerTotalStatus.Pending).ToListAsync(transactionCt);
   affected = totals.Count;
   if (totals.Count > 0 && request.PendingAction == DeliveryPendingAction.Cancel)
   {
    var totalIds = totals.Select(t => t.Id).ToList();
    var eventsByClosure = closures.ToDictionary(
     closure => closure.Id,
     closure => closure.Items.Select(item => item.BzaEventId).ToHashSet());
    var paymentPairs = totals
     .SelectMany(total => eventsByClosure[total.BzaClosureEventId]
      .Select(eventId => (total.BzaCustomerId, EventId: eventId)))
     .ToHashSet();
    var eventIds = paymentPairs.Select(pair => pair.EventId).Distinct().ToList();
    var customerIds = paymentPairs.Select(pair => pair.BzaCustomerId).Distinct().ToList();
    var paymentCandidates = await context.Payments
     .Where(p => customerIds.Contains(p.BzaCustomerId)
      && eventIds.Contains(p.BzaEventId)
      && p.PaymentMethod == "Comprobante"
      && !p.IsVerified)
     .ToListAsync(transactionCt);
    var payments = paymentCandidates
     .Where(payment => paymentPairs.Contains((payment.BzaCustomerId, payment.BzaEventId)))
     .ToList();
    if (payments.Count > 0) context.Payments.RemoveRange(payments); var now = DateTime.UtcNow; const string reason = "No se subió el comprobante antes de marcar los eventos en proceso de entrega.";
    var existingCancellationTotalIds = await context.SaleCancellations.Where(c => totalIds.Contains(c.BzaClosureCustomerTotalId)).Select(c => c.BzaClosureCustomerTotalId).ToListAsync(transactionCt);
    foreach (var total in totals) { if (!existingCancellationTotalIds.Contains(total.Id)) { var urls = total.Proofs.OrderBy(p => p.UploadedAt).Select(p => p.ImageUrl).ToList(); if (urls.Count == 0 && !string.IsNullOrWhiteSpace(total.ProofImageUrl)) urls.Add(total.ProofImageUrl);
      context.SaleCancellations.Add(new BzaSaleCancellation { TenantId = total.TenantId, BzaClosureCustomerTotalId = total.Id, BzaClosureEventId = total.BzaClosureEventId, BzaCustomerId = total.BzaCustomerId, CustomerName = total.Customer?.Name ?? "Cliente", CustomerPhone = total.Customer?.Phone, EventDescription = total.ClosureEvent.Description, TotalAmount = total.TotalAmount, Reason = reason, IsCustomerFault = true, CancelledAt = now, ProofUrls = urls.Count > 0 ? string.Join('\n', urls) : null }); }
     total.Status = BzaClosureCustomerTotalStatus.Cancelled; total.CancellationReason = reason; total.CancelledIsCustomerFault = true; total.CancelledAt = now; }
   }
   else if (totals.Count > 0)
   {
    var duplicateCustomer = totals
     .GroupBy(total => total.BzaCustomerId)
     .FirstOrDefault(group => group.Count() > 1);
    if (duplicateCustomer is not null)
     throw new InvalidOperationException("Un cliente tiene pendientes en más de un cierre seleccionado. Muévelos por separado para evitar duplicar su total.");

    BzaClosureEvent target;
    if (request.PendingAction == DeliveryPendingAction.MoveToExisting) { if (!request.TargetClosureEventId.HasValue || ids.Contains(request.TargetClosureEventId.Value)) throw new InvalidOperationException("El cierre destino no puede ser uno de los cierres seleccionados."); target = await context.ClosureEvents.Include(c => c.Items).Include(c => c.GroupDeliveries).Include(c => c.CustomerTotals).FirstOrDefaultAsync(c => c.Id == request.TargetClosureEventId.Value, transactionCt) ?? throw new KeyNotFoundException("El cierre destino no existe."); if (target.Status == BzaClosureEventStatus.Cancelled || target.InDeliveryProcess || target.Delivered) throw new InvalidOperationException("El cierre destino no está disponible."); }
    else { if (!request.NewDeliveryDate.HasValue || !request.NewPaymentDeadline.HasValue) throw new InvalidOperationException("Indica las fechas del nuevo cierre."); target = new BzaClosureEvent { Description = $"Pendientes movidos — Entrega {request.NewDeliveryDate.Value:dd/MM/yyyy}", OfficialDeliveryDate = request.NewDeliveryDate.Value.Date, PaymentDeadline = request.NewPaymentDeadline.Value, Status = BzaClosureEventStatus.PendingPayment }; context.ClosureEvents.Add(target); await context.SaveChangesAsync(transactionCt); }
    var customerIds = totals.Select(t => t.BzaCustomerId).Distinct().ToList(); var pendingPairs = totals.Select(t => (t.BzaClosureEventId, t.BzaCustomerId)).ToHashSet(); var candidateSales = await context.Sales.Where(s => s.BzaClosureEventId.HasValue && ids.Contains(s.BzaClosureEventId.Value) && customerIds.Contains(s.BzaCustomerId)).ToListAsync(transactionCt); var sales = candidateSales.Where(s => pendingPairs.Contains((s.BzaClosureEventId!.Value, s.BzaCustomerId))).ToList();
    var saleEventIds = sales.Select(s => s.BzaEventId).Distinct().ToList(); var existingEventIds = target.Items.Select(i => i.BzaEventId).ToHashSet(); foreach (var eventId in saleEventIds.Where(id => !existingEventIds.Contains(id))) target.Items.Add(new BzaClosureEventItem { BzaEventId = eventId });
    if (request.PendingAction == DeliveryPendingAction.MoveToNew) { var groupIds = totals.Where(t => t.BzaCollectorGroupId.HasValue).Select(t => t.BzaCollectorGroupId!.Value).Distinct(); foreach (var groupId in groupIds) target.GroupDeliveries.Add(new BzaClosureGroupDelivery { BzaCollectorGroupId = groupId, DeliveryDate = request.NewDeliveryDate!.Value.Date }); }
    const string mergeReason = "Pendiente integrado a un cliente existente en el cierre destino."; foreach (var total in totals) { var existingTotal = target.CustomerTotals.FirstOrDefault(existing => existing.BzaCustomerId == total.BzaCustomerId && existing.Status != BzaClosureCustomerTotalStatus.Cancelled); if (existingTotal is null) { total.BzaClosureEventId = target.Id; } else { existingTotal.TotalAmount += total.TotalAmount; total.Status = BzaClosureCustomerTotalStatus.Cancelled; total.CancellationReason = mergeReason; total.CancelledIsCustomerFault = false; total.CancelledAt = DateTime.UtcNow; } } foreach (var sale in sales) sale.BzaClosureEventId = target.Id; if (target.Status == BzaClosureEventStatus.Validated) target.Status = BzaClosureEventStatus.PendingPayment; targetId = target.Id;
   }
   foreach (var closure in closures) { closure.InDeliveryProcess = true; closure.DeliveryBatchId = deliveryBatchId; } await context.SaveChangesAsync(transactionCt);
  }, ct);
  return new(ids, affected, targetId);
 }
}