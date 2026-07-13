using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CommitBzaImport;

public class CommitBzaImportHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<CommitBzaImportCommand, CommitBzaImportResult>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<CommitBzaImportResult> Handle(CommitBzaImportCommand request, CancellationToken ct)
    {
        var result = new CommitBzaImportResult();

        // 1. Validar evento
        var saleEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == request.EventId, ct)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        if (saleEvent.Status == 5)
            throw new InvalidOperationException("No se puede importar en un evento cancelado.");

        // Si el evento ya está en proceso de pago (tiene ventas enviadas a cobro),
        // no se pueden agregar más ventas mediante el importador.
        var eventInPayment = await _context.Sales
            .AnyAsync(s => s.BzaEventId == request.EventId && s.BzaClosureEventId != null, ct);
        if (eventInPayment)
            throw new InvalidOperationException(
                "El evento ya está en proceso de pago (se enviaron totales a los clientes). No se pueden agregar más ventas a este evento.");

        // 2. Catálogos en memoria
        var collectors = await _context.Collectors.ToListAsync(ct);
        var collectorByName = new Dictionary<string, BzaCollector>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in collectors)
            collectorByName.TryAdd(c.Name.Trim(), c);

        // 2.a. Dar de alta los recolectores NUEVOS (solo los que no existen) con su grupo
        foreach (var nc in request.NewCollectors)
        {
            var name = nc.Name?.Trim();
            if (string.IsNullOrEmpty(name) || collectorByName.ContainsKey(name))
                continue;

            var groupExists = await _context.CollectorGroups.AnyAsync(g => g.Id == nc.GroupId, ct);
            if (!groupExists)
            {
                result.Errors.Add($"Grupo inválido para el recolector nuevo '{name}'.");
                continue;
            }

            var collector = new BzaCollector
            {
                Name = name,
                BzaCollectorGroupId = nc.GroupId,
                IsActive = true,
            };
            _context.Collectors.Add(collector);
            await _context.SaveChangesAsync(ct);
            collectorByName[name] = collector;
            result.NewCollectorsCreated++;
        }

        BzaCollector? ResolveCollector(string? name) =>
            string.IsNullOrWhiteSpace(name) ? null
            : collectorByName.TryGetValue(name.Trim(), out var c) ? c : null;

        // Ventas existentes del evento, indexadas por cliente
        var salesByCustomer = (await _context.Sales
            .Where(s => s.BzaEventId == request.EventId)
            .ToListAsync(ct))
            .ToDictionary(s => s.BzaCustomerId);

        // Índice de teléfonos ya usados (por tenant): el teléfono es único por tenant,
        // por eso se valida antes de crear/actualizar y así poder ignorar el registro
        // conflictivo sin abortar toda la importación.
        var phoneOwners = (await _context.Customers
                .Where(c => c.Phone != null && c.Phone != "")
                .Select(c => new { c.Id, c.Name, c.Phone })
                .ToListAsync(ct))
            .GroupBy(c => c.Phone.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var customerDto in request.Customers)
        {
            if (customerDto.Products.Count == 0)
                continue;

            // 3. Resolver / crear el cliente
            BzaCustomer? customer = null;

            if (customerDto.CustomerId is int existingId)
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == existingId, ct);
                if (customer is null)
                {
                    result.Errors.Add($"Cliente con Id {existingId} no encontrado.");
                    continue;
                }

                // 3.a. Cambio de recolector confirmado para cliente existente
                if (!string.IsNullOrWhiteSpace(customerDto.ChangeCollectorToName))
                {
                    var target = ResolveCollector(customerDto.ChangeCollectorToName);
                    if (target is null)
                    {
                        result.Errors.Add($"Recolector '{customerDto.ChangeCollectorToName}' no encontrado para '{customer.Name}'.");
                    }
                    else if (customer.BzaCollectorId != target.Id)
                    {
                        customer.BzaCollectorId = target.Id;
                        result.CollectorsChanged++;
                    }
                }

                // 3.b. Cambio de datos del cliente (Facebook / Teléfono) confirmado
                var customerUpdated = false;

                if (customerDto.ChangeFacebookNameTo is not null)
                {
                    var newFacebook = string.IsNullOrWhiteSpace(customerDto.ChangeFacebookNameTo)
                        ? null
                        : customerDto.ChangeFacebookNameTo.Trim();
                    if (!string.Equals(customer.FacebookName, newFacebook, StringComparison.Ordinal))
                    {
                        customer.FacebookName = newFacebook;
                        customerUpdated = true;
                    }
                }

                if (customerDto.ChangePhoneTo is not null)
                {
                    var newPhone = customerDto.ChangePhoneTo.Trim();
                    if (!string.Equals(customer.Phone, newPhone, StringComparison.Ordinal))
                    {
                        // El teléfono es único por tenant: si ya pertenece a OTRO cliente,
                        // no se aplica el cambio y se informa (no se ignora la venta).
                        if (newPhone.Length > 0
                            && phoneOwners.TryGetValue(newPhone, out var phoneOwner)
                            && phoneOwner.Id != customer.Id)
                        {
                            result.Errors.Add(
                                $"Cliente '{customer.Name}': no se cambió el teléfono a '{newPhone}' porque ya está registrado " +
                                $"para el cliente '{phoneOwner.Name}'. Se conservó su teléfono actual.");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(customer.Phone))
                                phoneOwners.Remove(customer.Phone.Trim());
                            customer.Phone = newPhone;
                            if (newPhone.Length > 0)
                                phoneOwners[newPhone] = new { customer.Id, customer.Name, Phone = newPhone };
                            customerUpdated = true;
                        }
                    }
                }

                if (customerUpdated)
                    result.CustomersUpdated++;
            }
            else if (customerDto.NewCustomer is { } nc)
            {
                if (string.IsNullOrWhiteSpace(nc.Name))
                {
                    result.Errors.Add("Cliente nuevo sin nombre.");
                    continue;
                }

                var collector = ResolveCollector(nc.CollectorName);
                if (collector is null)
                {
                    result.Errors.Add($"Recolector '{nc.CollectorName}' inválido para el cliente '{nc.Name}'.");
                    continue;
                }

                var newCustomerPhone = nc.Phone?.Trim() ?? string.Empty;

                // El teléfono es único por tenant. Si ya pertenece a otro cliente se IGNORA
                // este registro (no se crea el cliente ni su venta) y se detalla el conflicto,
                // permitiendo que el resto de la importación continúe.
                if (newCustomerPhone.Length > 0
                    && phoneOwners.TryGetValue(newCustomerPhone, out var owner))
                {
                    result.Errors.Add(
                        $"Cliente '{nc.Name.Trim()}' IGNORADO: el teléfono '{newCustomerPhone}' ya está registrado " +
                        $"para el cliente '{owner.Name}'. Corrige el teléfono y vuelve a importar este registro.");
                    result.IgnoredRecords++;
                    continue;
                }

                customer = new BzaCustomer
                {
                    Name = nc.Name.Trim(),
                    Phone = newCustomerPhone,
                    FacebookName = string.IsNullOrWhiteSpace(nc.FacebookName) ? null : nc.FacebookName.Trim(),
                    BzaCollectorId = collector.Id,
                    Status = 1,
                    PortalToken = Guid.NewGuid().ToString("N")[..12],
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync(ct);
                result.NewCustomersCreated++;

                if (newCustomerPhone.Length > 0)
                    phoneOwners[newCustomerPhone] = new { customer.Id, customer.Name, Phone = newCustomerPhone };
            }
            else
            {
                result.Errors.Add("Cliente sin identificar (ni existente ni nuevo).");
                continue;
            }

            // 4. Obtener o crear la venta del cliente en este evento (marcada como Excel)
            if (!salesByCustomer.TryGetValue(customer.Id, out var sale))
            {
                sale = new BzaSale
                {
                    BzaEventId = request.EventId,
                    BzaCustomerId = customer.Id,
                    Source = BzaSaleSource.Excel,
                };
                _context.Sales.Add(sale);
                salesByCustomer[customer.Id] = sale;
                result.SalesCreated++;
            }

            // 5. Agregar productos
            foreach (var p in customerDto.Products)
            {
                if (string.IsNullOrWhiteSpace(p.Description) || p.Price <= 0)
                {
                    result.Errors.Add($"Producto inválido para '{customer.Name}': '{p.Description}'.");
                    continue;
                }

                _context.SoldProducts.Add(new BzaSoldProduct
                {
                    Sale = sale,
                    Description = p.Description.Trim(),
                    Price = p.Price,
                });
                result.ImportedProducts++;
            }
        }

        await _context.SaveChangesAsync(ct);

        // 6. Auditoría en MongoDB (fire-and-forget con manejo de errores aislado)
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_ProductsImportedFromExcel",
            SaleEventId = saleEvent.Id,
            SaleEventDescription = saleEvent.Description,
            result.ImportedProducts,
            result.NewCustomersCreated,
            result.NewCollectorsCreated,
            result.CollectorsChanged,
            result.CustomersUpdated,
            result.SalesCreated,
            result.IgnoredRecords,
            DuplicateConfirmed = request.ConfirmDuplicate,
            Source = "Excel",
            Timestamp = DateTime.UtcNow,
        }, ct);

        return result;
    }
}
