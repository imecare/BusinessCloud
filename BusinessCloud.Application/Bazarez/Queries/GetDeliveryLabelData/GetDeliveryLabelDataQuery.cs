using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetDeliveryLabelData;

/// <summary>
/// Datos necesarios para generar etiquetas y hoja de despacho de un evento de entrega
/// (Cierre de Venta): identidad del bazar, grupos participantes y clientes con sus
/// productos.
/// </summary>
public record GetDeliveryLabelDataQuery(int ClosureEventId) : IRequest<DeliveryLabelDataDto>;

public record LabelBazarInfoDto(
    string? BazarName,
    string? LogoUrl,
    string? PhysicalAddress,
    string? FacebookPageUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LabelTagline,
    List<string> FacebookProfiles,
    List<string> Phones);

public record LabelGroupDto(
    int? GroupId,
    string GroupName,
    int CustomerCount);

public record LabelProductDto(
    int Id,
    string Description,
    decimal Price);

public record LabelCustomerDto(
    int CustomerId,
    string CustomerName,
    int? GroupId,
    string GroupName,
    string CollectorName,
    DateTime? DeliveryDate,
    decimal TotalAmount,
    int ProductCount,
    List<LabelProductDto> Products);

public class DeliveryLabelDataDto
{
    public int ClosureEventId { get; set; }
    public string EventDescription { get; set; } = string.Empty;
    public DateTime? OfficialDeliveryDate { get; set; }
    public bool InDeliveryProcess { get; set; }
    public LabelBazarInfoDto Bazar { get; set; } = new(null, null, null, null, null, null, null, new(), new());
    public List<LabelGroupDto> Groups { get; set; } = new();
    public List<LabelCustomerDto> Customers { get; set; } = new();
}

public class GetDeliveryLabelDataHandler(IBazaresDbContext context)
    : IRequestHandler<GetDeliveryLabelDataQuery, DeliveryLabelDataDto>
{
    private const string NoGroupLabel = "Sin grupo";

    public async Task<DeliveryLabelDataDto> Handle(GetDeliveryLabelDataQuery request, CancellationToken ct)
    {
        var closure = await context.ClosureEvents
            .AsNoTracking()
            .Include(c => c.CustomerTotals)
                .ThenInclude(t => t.Customer)
                    .ThenInclude(cu => cu.Collector)
            .Include(c => c.GroupDeliveries)
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, ct)
            ?? throw new KeyNotFoundException("El evento de entrega no existe.");

        // Fecha de entrega por grupo (si no hay, se usa la fecha oficial del cierre).
        var deliveryByGroup = closure.GroupDeliveries
            .GroupBy(g => g.BzaCollectorGroupId)
            .ToDictionary(g => g.Key, g => g.First().DeliveryDate);

        // Nombres de grupo.
        var groupIds = closure.CustomerTotals
            .Where(t => t.BzaCollectorGroupId.HasValue)
            .Select(t => t.BzaCollectorGroupId!.Value)
            .Distinct()
            .ToList();

        var groupNames = await context.CollectorGroups
            .AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Description, ct);

        // Productos por cliente: ventas ligadas a este cierre.
        var sales = await context.Sales
            .AsNoTracking()
            .Where(s => s.BzaClosureEventId == request.ClosureEventId)
            .Include(s => s.Products)
            .ToListAsync(ct);

        var productsByCustomer = sales
            .GroupBy(s => s.BzaCustomerId)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(s => s.Products)
                      .Select(p => new LabelProductDto(p.Id, p.Description, p.Price))
                      .ToList());

        // Clientes.
        var customers = closure.CustomerTotals
            .Select(t =>
            {
                var groupName = t.BzaCollectorGroupId.HasValue && groupNames.TryGetValue(t.BzaCollectorGroupId.Value, out var gn)
                    ? gn
                    : NoGroupLabel;
                var products = productsByCustomer.TryGetValue(t.BzaCustomerId, out var list)
                    ? list
                    : new List<LabelProductDto>();
                var deliveryDate = t.BzaCollectorGroupId.HasValue && deliveryByGroup.TryGetValue(t.BzaCollectorGroupId.Value, out var dd)
                    ? dd
                    : closure.OfficialDeliveryDate;
                return new LabelCustomerDto(
                    t.BzaCustomerId,
                    t.Customer != null ? t.Customer.Name : "Cliente",
                    t.BzaCollectorGroupId,
                    groupName,
                    t.Customer?.Collector?.Name ?? string.Empty,
                    deliveryDate,
                    t.TotalAmount,
                    products.Count,
                    products);
            })
            .OrderBy(c => c.GroupName)
            .ThenBy(c => c.CustomerName)
            .ToList();

        // Grupos participantes (con conteo de clientes).
        var groups = customers
            .GroupBy(c => new { c.GroupId, c.GroupName })
            .Select(g => new LabelGroupDto(g.Key.GroupId, g.Key.GroupName, g.Count()))
            .OrderBy(g => g.GroupName)
            .ToList();

        // Identidad del bazar.
        var settings = await context.BazarSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        LabelBazarInfoDto bazar;
        if (settings is null)
        {
            bazar = new LabelBazarInfoDto(null, null, null, null, null, null, null, new(), new());
        }
        else
        {
            var phones = await context.ContactPhones
                .AsNoTracking()
                .Where(p => p.BzaBazarSettingsId == settings.Id)
                .OrderBy(p => p.Id)
                .Select(p => p.PhoneNumber)
                .ToListAsync(ct);

            var profiles = await context.FacebookProfiles
                .AsNoTracking()
                .Where(p => p.BzaBazarSettingsId == settings.Id)
                .OrderBy(p => p.Id)
                .Select(p => p.ProfileUrl)
                .ToListAsync(ct);

            bazar = new LabelBazarInfoDto(
                settings.BazarName,
                settings.LogoUrl,
                settings.PhysicalAddress,
                settings.FacebookPageUrl,
                settings.PrimaryColor,
                settings.SecondaryColor,
                settings.LabelTagline,
                profiles,
                phones);
        }

        return new DeliveryLabelDataDto
        {
            ClosureEventId = closure.Id,
            EventDescription = closure.Description,
            OfficialDeliveryDate = closure.OfficialDeliveryDate,
            InDeliveryProcess = closure.InDeliveryProcess,
            Bazar = bazar,
            Groups = groups,
            Customers = customers
        };
    }
}
