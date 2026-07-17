using BusinessCloud.Application.Admin.Commands.NotifyExpiringCompanies;
using BusinessCloud.Application.Admin.Commands.PurchasePackage;
using BusinessCloud.Application.Admin.Commands.RegisterCompanyPayment;
using BusinessCloud.Application.Admin.Commands.SetCompanyStatus;
using BusinessCloud.Application.Admin.Commands.UpsertSubscription;
using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Admin.Queries.GetCompanies;
using BusinessCloud.Application.Admin.Queries.GetCompanyById;
using BusinessCloud.Application.Admin.Queries.GetExpirationAlerts;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Api.Controllers.Admin;

/// <summary>
/// Panel de administración del SaaS: gestión de empresas (Tenants) y sus suscripciones.
/// Requiere el rol global PlatformAdmin (cross-tenant).
/// </summary>
[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/companies")]
public class AdminCompaniesController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityDbContext _identityContext;

    public AdminCompaniesController(
        ISender mediator,
        UserManager<ApplicationUser> userManager,
        IdentityDbContext identityContext)
    {
        _mediator = mediator;
        _userManager = userManager;
        _identityContext = identityContext;
    }

    /// <summary>Lista las empresas con el estado de su suscripción.</summary>
    [HttpGet]
    public async Task<IActionResult> GetCompanies([FromQuery] string? search, [FromQuery] string? status)
    {
        var companies = await _mediator.Send(new GetCompaniesQuery(search, status));
        return Ok(new ApiResponse<IReadOnlyList<Application.Admin.Dtos.CompanyListItemDto>>
        {
            Success = true,
            Data = companies
        });
    }

    /// <summary>Obtiene el detalle de una empresa y su suscripción.</summary>
    [HttpGet("{tenantId}")]
    public async Task<IActionResult> GetCompany(string tenantId)
    {
        var company = await _mediator.Send(new GetCompanyByIdQuery(tenantId));
        if (company is null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Empresa no encontrada." });

        return Ok(new ApiResponse<CompanyDetailDto> { Success = true, Data = company });
    }

    /// <summary>Contadores de vencimiento y empresas que requieren atención (avisos).</summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts([FromQuery] int expiringSoonDays = 10)
    {
        var alerts = await _mediator.Send(new GetExpirationAlertsQuery(expiringSoonDays));
        return Ok(new ApiResponse<ExpirationAlertsDto> { Success = true, Data = alerts });
    }

    /// <summary>Envía avisos por WhatsApp a los dueños de empresas por vencer / vencidas.</summary>
    [HttpPost("notify-expirations")]
    public async Task<IActionResult> NotifyExpirations([FromQuery] int expiringSoonDays = 10)
    {
        var result = await _mediator.Send(new NotifyExpiringCompaniesCommand(expiringSoonDays));
        return Ok(new ApiResponse<NotifyExpiringCompaniesResult>
        {
            Success = true,
            Message = $"Avisos enviados: {result.Notified}. Fallidos: {result.Failed}.",
            Data = result
        });
    }

    /// <summary>Da de alta una empresa: tenant, módulos, usuario SuperAdmin y (opcional) suscripción.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return BadRequest(new ApiResponse<object> { Success = false, Message = "El nombre de la empresa es obligatorio." });

        if (string.IsNullOrWhiteSpace(request.AdminEmail) || string.IsNullOrWhiteSpace(request.AdminPassword))
            return BadRequest(new ApiResponse<object> { Success = false, Message = "El correo y la contraseña del administrador son obligatorios." });

        var emailExists = await _userManager.FindByEmailAsync(request.AdminEmail);
        if (emailExists is not null)
            return Conflict(new ApiResponse<object> { Success = false, Message = "El correo del administrador ya está registrado." });

        var tenantId = Guid.NewGuid().ToString()[..8];
        var tenant = new Tenant { Id = tenantId, Name = request.CompanyName.Trim() };
        _identityContext.Tenants.Add(tenant);

        var modulesToActivate = request.Modules?.Length > 0
            ? request.Modules.Where(m => SystemModules.All.Contains(m)).ToArray()
            : SystemModules.All;

        foreach (var module in modulesToActivate)
        {
            _identityContext.TenantModules.Add(new TenantModule
            {
                TenantId = tenantId,
                Module = module,
                IsActive = true
            });
        }

        await _identityContext.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = request.AdminEmail,
            Email = request.AdminEmail,
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            TenantId = tenantId,
            Role = SystemRoles.SuperAdmin,
            IsActive = true,
            MustChangePassword = true
        };

        var result = await _userManager.CreateAsync(user, request.AdminPassword);
        if (!result.Succeeded)
        {
            // Rollback del tenant y sus módulos.
            var modulesToRemove = _identityContext.TenantModules.Where(m => m.TenantId == tenantId);
            _identityContext.TenantModules.RemoveRange(modulesToRemove);
            var tenantToRemove = await _identityContext.Tenants.FindAsync(tenantId);
            if (tenantToRemove is not null)
                _identityContext.Tenants.Remove(tenantToRemove);
            await _identityContext.SaveChangesAsync();

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = string.Join(" ", result.Errors.Select(e => e.Description))
            });
        }

        // Suscripción inicial opcional.
        if (request.Subscription is not null)
        {
            var s = request.Subscription;
            await _mediator.Send(new UpsertSubscriptionCommand
            {
                TenantId = tenantId,
                PlanName = s.PlanName,
                Period = s.Period,
                Price = s.Price,
                Currency = s.Currency,
                PaidUntil = s.PaidUntil,
                GraceDays = s.GraceDays,
                OwnerName = s.OwnerName,
                OwnerPhone = s.OwnerPhone,
                SellerId = s.SellerId,
                CommissionInitialAmount = s.CommissionInitialAmount,
                CommissionMonthlyPercent = s.CommissionMonthlyPercent,
                Notes = s.Notes
            });
        }

        var detail = await _mediator.Send(new GetCompanyByIdQuery(tenantId));
        return CreatedAtAction(nameof(GetCompany), new { tenantId }, new ApiResponse<CompanyDetailDto>
        {
            Success = true,
            Message = "Empresa creada con éxito.",
            Data = detail
        });
    }

    /// <summary>Crea o actualiza la suscripción de una empresa.</summary>
    [HttpPut("{tenantId}/subscription")]
    public async Task<IActionResult> UpsertSubscription(string tenantId, [FromBody] SubscriptionInput input)
    {
        var subscriptionId = await _mediator.Send(new UpsertSubscriptionCommand
        {
            TenantId = tenantId,
            PlanName = input.PlanName,
            Period = input.Period,
            Price = input.Price,
            Currency = input.Currency,
            PaidUntil = input.PaidUntil,
            GraceDays = input.GraceDays,
            OwnerName = input.OwnerName,
            OwnerPhone = input.OwnerPhone,
            SellerId = input.SellerId,
            CommissionInitialAmount = input.CommissionInitialAmount,
            CommissionMonthlyPercent = input.CommissionMonthlyPercent,
            Notes = input.Notes
        });

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Suscripción actualizada.",
            Data = new { subscriptionId }
        });
    }

    /// <summary>Registra un pago y extiende la fecha pagada de la empresa.</summary>
    [HttpPost("{tenantId}/payment")]
    public async Task<IActionResult> RegisterPayment(string tenantId, [FromBody] RegisterPaymentRequest request)
    {
        var result = await _mediator.Send(new RegisterCompanyPaymentCommand
        {
            TenantId = tenantId,
            Periods = request.Periods,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            Notes = request.Notes
        });

        return Ok(new ApiResponse<RegisterCompanyPaymentResult>
        {
            Success = true,
            Message = "Pago registrado.",
            Data = result
        });
    }

    /// <summary>Activa o suspende una empresa.</summary>
    [HttpPut("{tenantId}/status")]
    public async Task<IActionResult> SetStatus(string tenantId, [FromBody] SetCompanyStatusRequest request)
    {
        var result = await _mediator.Send(new SetCompanyStatusCommand(tenantId, request.IsActive));
        return Ok(new ApiResponse<SetCompanyStatusResult>
        {
            Success = true,
            Message = request.IsActive ? "Empresa activada." : "Empresa suspendida.",
            Data = result
        });
    }

    /// <summary>Registra la compra de un paquete o mensajes adicionales (acumula mensajes).</summary>
    [HttpPost("{tenantId}/purchase-package")]
    public async Task<IActionResult> PurchasePackage(string tenantId, [FromBody] PurchasePackageRequest request)
    {
        var result = await _mediator.Send(new PurchasePackageCommand
        {
            TenantId = tenantId,
            PackageId = request.PackageId,
            CustomMessages = request.CustomMessages,
            CustomPrice = request.CustomPrice,
            Note = request.Note,
        });

        return Ok(new ApiResponse<PurchasePackageResult>
        {
            Success = true,
            Message = $"Se agregaron {result.MessagesAdded} mensaje(s). Disponibles: {result.Available}.",
            Data = result
        });
    }
}
