using BusinessCloud.Application.Bazares.Queries.GetDeliveryLabelData;
using BusinessCloud.Application.Bazares.Queries.GetPendingMoveOptions;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace BusinessCloud.Application.Bazares.Queries.GetDeliveryLogisticsBatch;
public record GetDeliveryLogisticsBatchQuery(
    List<int> ClosureEventIds,
    bool IncludeProcessed = false) : IRequest<DeliveryLogisticsBatchDto>;
public record DeliveryLogisticsClosureDto(int ClosureEventId, string Description, DateTime? OfficialDeliveryDate);
public record DeliveryLogisticsCustomerDto(int ClosureTotalId, int ClosureEventId, string EventDescription, int CustomerId, string CustomerName, int? GroupId, string GroupName, string CollectorName, DateTime? DeliveryDate, decimal TotalAmount, int ProductCount, List<LabelProductDto> Products, int Status);
public class DeliveryLogisticsBatchDto
{
 public List<DeliveryLogisticsClosureDto> Closures { get; set; } = new();
 public LabelBazarInfoDto Bazar { get; set; } = new(null, null, null, null, null, null, null, new(), new());
 public List<LabelGroupDto> Groups { get; set; } = new();
 public List<DeliveryLogisticsCustomerDto> Customers { get; set; } = new();
 public int PendingCount { get; set; }
 public List<PendingMoveCandidateDto> PendingMoveCandidates { get; set; } = new();
}
public class GetDeliveryLogisticsBatchHandler(IBazaresDbContext context) : IRequestHandler<GetDeliveryLogisticsBatchQuery, DeliveryLogisticsBatchDto>
{
 public async Task<DeliveryLogisticsBatchDto> Handle(GetDeliveryLogisticsBatchQuery request, CancellationToken ct)
 {
  var ids = request.ClosureEventIds.Distinct().ToList();
  var closures = await context.ClosureEvents.AsNoTracking().Include(c => c.CustomerTotals).ThenInclude(t => t.Customer).ThenInclude(c => c.Collector).Include(c => c.GroupDeliveries)
   .Where(c => ids.Contains(c.Id)
    && c.Status != BzaClosureEventStatus.Cancelled
    && (request.IncludeProcessed || (!c.InDeliveryProcess && !c.Delivered))
    && c.CustomerTotals.Any(t => t.Status == BzaClosureCustomerTotalStatus.Validated)
    && context.Sales.Any(s => s.BzaClosureEventId == c.Id)).ToListAsync(ct);
  if (closures.Count != ids.Count) throw new InvalidOperationException("Uno o más cierres ya no son viables para logística.");
  var totals = closures.SelectMany(c => c.CustomerTotals).Where(t => t.Status == BzaClosureCustomerTotalStatus.Validated).ToList();
  if (totals.Count == 0) throw new InvalidOperationException("Los cierres seleccionados no tienen clientes con pago validado.");
  var groupIds = totals.Where(t => t.BzaCollectorGroupId.HasValue).Select(t => t.BzaCollectorGroupId!.Value).Distinct().ToList();
  var groupNames = await context.CollectorGroups.AsNoTracking().Where(g => groupIds.Contains(g.Id)).ToDictionaryAsync(g => g.Id, g => g.Description, ct);
  var sales = await context.Sales.AsNoTracking().Include(s => s.Products).Where(s => s.BzaClosureEventId.HasValue && ids.Contains(s.BzaClosureEventId.Value)).ToListAsync(ct);
  var products = sales.GroupBy(s => (s.BzaClosureEventId!.Value, s.BzaCustomerId)).ToDictionary(g => g.Key, g => g.SelectMany(s => s.Products).Select(p => new LabelProductDto(p.Id, p.Description, p.Price)).ToList());
  var closureById = closures.ToDictionary(c => c.Id);
  var customers = totals.Select(t => { var closure = closureById[t.BzaClosureEventId]; var groupName = t.BzaCollectorGroupId.HasValue && groupNames.TryGetValue(t.BzaCollectorGroupId.Value, out var name) ? name : "Sin grupo";
   var customerProducts = products.GetValueOrDefault((t.BzaClosureEventId, t.BzaCustomerId)) ?? new(); var deliveryDate = t.BzaCollectorGroupId.HasValue ? closure.GroupDeliveries.FirstOrDefault(g => g.BzaCollectorGroupId == t.BzaCollectorGroupId.Value)?.DeliveryDate ?? closure.OfficialDeliveryDate : closure.OfficialDeliveryDate;
   return new DeliveryLogisticsCustomerDto(t.Id, t.BzaClosureEventId, closure.Description, t.BzaCustomerId, t.Customer?.Name ?? "Cliente", t.BzaCollectorGroupId, groupName, t.Customer?.Collector?.Name ?? string.Empty, deliveryDate, t.TotalAmount, customerProducts.Count, customerProducts, t.Status);
  }).OrderBy(c => c.DeliveryDate).ThenBy(c => c.GroupName).ThenBy(c => c.CustomerName).ToList();
  var settings = await context.BazarSettings.AsNoTracking().FirstOrDefaultAsync(ct); var bazar = new LabelBazarInfoDto(null, null, null, null, null, null, null, new(), new());
  if (settings is not null) { var phones = await context.ContactPhones.AsNoTracking().Where(p => p.BzaBazarSettingsId == settings.Id).OrderBy(p => p.Id).Select(p => p.PhoneNumber).ToListAsync(ct); var profiles = await context.FacebookProfiles.AsNoTracking().Where(p => p.BzaBazarSettingsId == settings.Id).OrderBy(p => p.Id).Select(p => p.ProfileUrl).ToListAsync(ct); bazar = new(settings.BazarName, settings.LogoUrl, settings.PhysicalAddress, settings.FacebookPageUrl, settings.PrimaryColor, settings.SecondaryColor, settings.LabelTagline, profiles, phones); }
  var today = DateTime.UtcNow.Date; var candidates = await context.ClosureEvents.AsNoTracking().Where(c => !ids.Contains(c.Id) && c.Status != BzaClosureEventStatus.Cancelled && !c.InDeliveryProcess && !c.Delivered && c.OfficialDeliveryDate != null && c.OfficialDeliveryDate.Value.Date >= today).OrderBy(c => c.OfficialDeliveryDate).Select(c => new PendingMoveCandidateDto(c.Id, c.Description, c.OfficialDeliveryDate, c.PaymentDeadline)).ToListAsync(ct);
  return new() { Closures = closures.OrderBy(c => c.OfficialDeliveryDate).Select(c => new DeliveryLogisticsClosureDto(c.Id, c.Description, c.OfficialDeliveryDate)).ToList(), Bazar = bazar, Customers = customers,
   Groups = customers.GroupBy(c => new { c.GroupId, c.GroupName }).Select(g => new LabelGroupDto(g.Key.GroupId, g.Key.GroupName, g.Count())).OrderBy(g => g.GroupName).ToList(), PendingCount = closures.SelectMany(c => c.CustomerTotals).Count(t => t.Status == BzaClosureCustomerTotalStatus.Pending), PendingMoveCandidates = candidates };
 }
}