using BusinessCloud.Application.Bazares.Commands.ProcessDeliveryBatch;
using BusinessCloud.Application.Bazares.Commands.UnifyDeliveryDates;
using BusinessCloud.Application.Bazares.Queries.GetDeliveryLogisticsEvents;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BusinessCloud.Tests.Application.Bazares;
public class DeliveryLogisticsBatchHandlerTests
{
 private const string Tenant = BazaresContextFactory.TenantId;
 [Fact]
 public async Task LogisticsEvents_ReturnsOnlySentClosuresWithSalesBeforeDelivery()
 {
  using var context = BazaresContextFactory.Create();
  context.ClosureEvents.AddRange(Closure(1), Closure(2, inDelivery: true), Closure(3));
  context.Sales.AddRange(new BzaSale { Id = 1, TenantId = Tenant, BzaClosureEventId = 1, BzaEventId = 10, BzaCustomerId = 10 }, new BzaSale { Id = 2, TenantId = Tenant, BzaClosureEventId = 2, BzaEventId = 20, BzaCustomerId = 20 });
  await context.SaveChangesAsync(default);
  var result = await new GetDeliveryLogisticsEventsHandler(context).Handle(new(), default);
  Assert.Equal([1], result.Select(item => item.Id).ToArray());
 }

 [Fact]
 public async Task UnifyDates_UpdatesOfficialAndEveryGroupDate()
 {
  using var context = BazaresContextFactory.Create();
  var first = Closure(1); first.GroupDeliveries.Add(new BzaClosureGroupDelivery { TenantId = Tenant, BzaCollectorGroupId = 1, DeliveryDate = new DateTime(2026, 9, 1) });
  var second = Closure(2); second.GroupDeliveries.Add(new BzaClosureGroupDelivery { TenantId = Tenant, BzaCollectorGroupId = 2, DeliveryDate = new DateTime(2026, 9, 8) });
  context.ClosureEvents.AddRange(first, second); await context.SaveChangesAsync(default);
  var date = DateTime.UtcNow.Date.AddDays(10);
  var result = await new UnifyDeliveryDatesHandler(context).Handle(new([1, 2], date), default);
  Assert.Equal(2, result.UpdatedGroupDates);
  Assert.All(await context.ClosureEvents.Include(c => c.GroupDeliveries).ToListAsync(), closure => { Assert.Equal(date, closure.OfficialDeliveryDate); Assert.All(closure.GroupDeliveries, group => Assert.Equal(date, group.DeliveryDate)); });
 }

 [Fact]
 public async Task ProcessBatch_CancelsAllPendingAndStartsEveryClosureAtomically()
 {
  using var context = BazaresContextFactory.Create();
  var customer1 = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana", Phone = "1" }; var customer2 = new BzaCustomer { Id = 2, TenantId = Tenant, Name = "Beto", Phone = "2" };
  var first = Closure(1); first.Items.Add(new BzaClosureEventItem { TenantId = Tenant, BzaEventId = 10 }); first.CustomerTotals.Add(Total(1, 1, customer1));
  var second = Closure(2); second.Items.Add(new BzaClosureEventItem { TenantId = Tenant, BzaEventId = 20 }); second.CustomerTotals.Add(Total(2, 2, customer2));
  context.Customers.AddRange(customer1, customer2); context.ClosureEvents.AddRange(first, second); await context.SaveChangesAsync(default);
  var result = await new ProcessDeliveryBatchHandler(context).Handle(new([1, 2], DeliveryPendingAction.Cancel), default);
  Assert.Equal(2, result.PendingAffected); Assert.All(await context.ClosureEvents.ToListAsync(), closure => Assert.True(closure.InDeliveryProcess));
  var totals = await context.ClosureCustomerTotals.ToListAsync();
  Assert.All(totals.Where(total => total.Id is 1 or 2), total => Assert.Equal(BzaClosureCustomerTotalStatus.Cancelled, total.Status));
  Assert.All(totals.Where(total => total.Id is not (1 or 2)), total => Assert.Equal(BzaClosureCustomerTotalStatus.Validated, total.Status));
  Assert.Equal(2, await context.SaleCancellations.CountAsync());
 }

 [Fact]
 public async Task ProcessBatch_CancelOnlyRemovesPaymentsFromThePendingSourceClosure()
 {
  using var context = BazaresContextFactory.Create();
  var customer = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana", Phone = "1" };
  var first = Closure(1);
  first.Items.Add(new BzaClosureEventItem { TenantId = Tenant, BzaEventId = 10 });
  first.CustomerTotals.Add(Total(1, customer.Id, customer));
  var second = Closure(2);
  second.Items.Add(new BzaClosureEventItem { TenantId = Tenant, BzaEventId = 20 });
  second.CustomerTotals.First().BzaCustomerId = customer.Id;
  context.Customers.Add(customer);
  context.ClosureEvents.AddRange(first, second);
  context.Payments.AddRange(
   new BzaPayment { Id = 1, TenantId = Tenant, BzaCustomerId = customer.Id, BzaEventId = 10, Amount = 100, PaymentMethod = "Comprobante", IsVerified = false, PaymentStatus = 1 },
   new BzaPayment { Id = 2, TenantId = Tenant, BzaCustomerId = customer.Id, BzaEventId = 20, Amount = 100, PaymentMethod = "Comprobante", IsVerified = false, PaymentStatus = 1 });
  await context.SaveChangesAsync(default);

  await new ProcessDeliveryBatchHandler(context).Handle(
   new([first.Id, second.Id], DeliveryPendingAction.Cancel),
   default);

  var remainingPayment = Assert.Single(await context.Payments.ToListAsync());
  Assert.Equal(20, remainingPayment.BzaEventId);
 }

 [Fact]
 public async Task ProcessBatch_MoveRejectsDuplicatePendingCustomerWithoutPartialChanges()
 {
  using var context = BazaresContextFactory.Create();
  var customer = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana", Phone = "1" };
  var first = Closure(1);
  first.CustomerTotals.Add(Total(1, customer.Id, customer));
  var second = Closure(2);
  second.CustomerTotals.Add(Total(2, customer.Id, customer));
  context.Customers.Add(customer);
  context.ClosureEvents.AddRange(first, second);
  await context.SaveChangesAsync(default);

  var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
   new ProcessDeliveryBatchHandler(context).Handle(
    new(
     [first.Id, second.Id],
     DeliveryPendingAction.MoveToNew,
     NewDeliveryDate: DateTime.UtcNow.Date.AddDays(10),
     NewPaymentDeadline: DateTime.UtcNow.Date.AddDays(8)),
    default));

  Assert.Contains("más de un cierre", exception.Message);
  Assert.All(await context.ClosureEvents.ToListAsync(), closure => Assert.False(closure.InDeliveryProcess));
 }

 private static BzaClosureEvent Closure(int id, bool inDelivery = false) => new() { Id = id, TenantId = Tenant, Description = $"Cierre {id}", PaymentDeadline = DateTime.UtcNow.AddDays(2), Status = BzaClosureEventStatus.ProofReceived, InDeliveryProcess = inDelivery, CustomerTotals = [new BzaClosureCustomerTotal { Id = id * 100, TenantId = Tenant, BzaCustomerId = id + 100, Status = BzaClosureCustomerTotalStatus.Validated, UploadToken = $"token-{id}" }] };
 private static BzaClosureCustomerTotal Total(int id, int customerId, BzaCustomer customer) => new() { Id = id, TenantId = Tenant, BzaCustomerId = customerId, Customer = customer, Status = BzaClosureCustomerTotalStatus.Pending, UploadToken = $"pending-{id}", TotalAmount = 100 };
}