using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CommitBzaCustomersImport;

public class CommitBzaCustomersImportHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<CommitBzaCustomersImportCommand, CommitBzaCustomersImportResult>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<CommitBzaCustomersImportResult> Handle(CommitBzaCustomersImportCommand request, CancellationToken ct)
    {
        var result = new CommitBzaCustomersImportResult();

        // 1. Catálogos en memoria
        var collectors = await _context.Collectors.ToListAsync(ct);
        var collectorByName = new Dictionary<string, BzaCollector>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in collectors)
            collectorByName.TryAdd(c.Name.Trim(), c);

        // 1.a. Dar de alta los recolectores NUEVOS (solo los que no existen) con su grupo
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

        // Índices para detectar duplicados de nombre y teléfono (únicos por tenant).
        var existing = await _context.Customers
            .Select(c => new { c.Id, c.Name, c.Phone })
            .ToListAsync(ct);

        var existingNames = new HashSet<string>(
            existing.Select(c => c.Name.Trim()), StringComparer.OrdinalIgnoreCase);

        var phoneOwners = existing
            .Where(c => !string.IsNullOrWhiteSpace(c.Phone))
            .GroupBy(c => c.Phone.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

        foreach (var dto in request.Customers)
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                result.Errors.Add("Cliente sin nombre. Se omitió.");
                result.IgnoredRecords++;
                continue;
            }

            // Ya existe un cliente con ese nombre -> se omite.
            if (existingNames.Contains(name))
            {
                result.Errors.Add($"Cliente '{name}' IGNORADO: ya está registrado.");
                result.IgnoredRecords++;
                continue;
            }

            var collector = ResolveCollector(dto.CollectorName);
            if (collector is null)
            {
                result.Errors.Add($"Cliente '{name}' IGNORADO: recolector '{dto.CollectorName}' inválido.");
                result.IgnoredRecords++;
                continue;
            }

            var phone = NormalizePhone(dto.Phone);

            // Teléfono único por tenant: si ya pertenece a otro cliente, se omite el registro.
            if (phone.Length > 0 && phoneOwners.TryGetValue(phone, out var owner))
            {
                result.Errors.Add(
                    $"Cliente '{name}' IGNORADO: el teléfono '{phone}' ya está registrado para el cliente '{owner}'. " +
                    "Corrige el teléfono y vuelve a importar este registro.");
                result.IgnoredRecords++;
                continue;
            }

            var customer = new BzaCustomer
            {
                Name = name,
                Phone = phone,
                FacebookName = string.IsNullOrWhiteSpace(dto.FacebookName) ? null : dto.FacebookName.Trim(),
                BzaCollectorId = collector.Id,
                Status = 1,
                PortalToken = Guid.NewGuid().ToString("N")[..12],
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync(ct);

            existingNames.Add(name);
            if (phone.Length > 0)
                phoneOwners[phone] = name;
            result.CustomersCreated++;
        }

        // Auditoría en MongoDB
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_CustomersImportedFromExcel",
            result.CustomersCreated,
            result.NewCollectorsCreated,
            result.IgnoredRecords,
            Source = "Excel",
            Timestamp = DateTime.UtcNow,
        }, ct);

        return result;
    }

    /// <summary>Deja solo los dígitos del teléfono para usarlo como llave única.</summary>
    private static string NormalizePhone(string? phone)
        => new((phone ?? string.Empty).Where(char.IsDigit).ToArray());
}
