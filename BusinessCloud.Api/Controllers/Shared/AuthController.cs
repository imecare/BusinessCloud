using BusinessCloud.Application.Auth.Commands.ConfirmPasswordRecoveryContact;
using BusinessCloud.Application.Auth.Commands.RequestPasswordRecovery;
using BusinessCloud.Application.Auth.Commands.ResetPasswordRecovery;
using BusinessCloud.Application.Auth.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Common.Services;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessCloud.Api.Controllers.Shared;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Numero de WhatsApp autorizado que recibe SIEMPRE los codigos de recuperacion
    /// de contrasena, sin importar la cuenta que solicite el restablecimiento.
    /// </summary>
    private const string ForgotPasswordWhatsAppNumber = "3121232192";

    /// <summary>Proposito del OTP para el flujo publico de recuperacion de contrasena.</summary>
    private const string ForgotPasswordPurpose = "password.forgot";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IdentityDbContext _identityContext;
    private readonly JwtTokenService _jwtService;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentsDbContext _paymentsDb;
    private readonly IWhatsAppSender _whatsApp;
    private readonly IVerificationCodeService _verification;
    private readonly IBazaresDbContext _bazaresDb;
    private readonly ILogger<AuthController> _logger;
    private readonly ISender _sender;
    private readonly IAdminPinService _adminPin;
    private readonly string _backendVersion;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IdentityDbContext identityContext,
        JwtTokenService jwtService,
        ICurrentUserService currentUser,
        IPaymentsDbContext paymentsDb,
        IWhatsAppSender whatsApp,
        IVerificationCodeService verification,
        IBazaresDbContext bazaresDb,
        ILogger<AuthController> logger,
        ISender sender,
        IAdminPinService adminPin,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _identityContext = identityContext;
        _jwtService = jwtService;
        _currentUser = currentUser;
        _paymentsDb = paymentsDb;
        _whatsApp = whatsApp;
        _verification = verification;
        _bazaresDb = bazaresDb;
        _logger = logger;
        _sender = sender;
        _adminPin = adminPin;
        _backendVersion = configuration["AppVersion"]
            ?? throw new InvalidOperationException("AppVersion no esta configurada.");
    }

    [HttpPost("register-company")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var tenantId = Guid.NewGuid().ToString().Substring(0, 8);
            var tenant = new Tenant
            {
                Id = tenantId,
                Name = request.CompanyName
            };

            _identityContext.Tenants.Add(tenant);

            // Activar m�dulos solicitados (o todos por defecto)
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
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                TenantId = tenantId,
                Role = "SuperAdmin",
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                // Rollback manual: eliminar tenant y m�dulos creados
                var tenantToRemove = await _identityContext.Tenants.FindAsync(tenantId);
                if (tenantToRemove != null)
                    _identityContext.Tenants.Remove(tenantToRemove);
                var modulesToRemove = _identityContext.TenantModules.Where(m => m.TenantId == tenantId);
                _identityContext.TenantModules.RemoveRange(modulesToRemove);
                await _identityContext.SaveChangesAsync();

                return BadRequest(result.Errors);
            }

            return Ok(new
            {
                Message = "Empresa y Usuario creados con �xito",
                TenantId = tenantId,
                Modules = modulesToActivate
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
            return Unauthorized(new { success = false, message = "Credenciales inv�lidas." });

        if (!user.IsActive)
            return Unauthorized(new { success = false, message = "Usuario desactivado. Contacte al administrador." });

        if (user.Role == "Commissionist" && !user.SellerId.HasValue)
            return BadRequest(new { success = false, message = "Comisionista sin vendedor asignado. Contacte al administrador." });

        // lockoutOnFailure: true -> cuenta los intentos fallidos y bloquea temporalmente (anti fuerza bruta)
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return StatusCode(423, new { success = false, message = "Cuenta bloqueada temporalmente por m�ltiples intentos fallidos. Intenta de nuevo en unos minutos." });

        if (!result.Succeeded)
            return Unauthorized(new { success = false, message = "Credenciales inv�lidas." });

        // Obtener m�dulos habilitados del tenant
        var modules = await _identityContext.TenantModules
            .Where(tm => tm.TenantId == user.TenantId && tm.IsActive)
            .Select(tm => tm.Module)
            .ToListAsync();

        var isPlatformAdmin = user.Role == SystemRoles.PlatformAdmin;

        // El PlatformAdmin es el administrador global del SaaS (cross-tenant): no pertenece a
        // ninguna empresa ni valida m�dulos de tenant; opera exclusivamente el panel Admin.
        if (isPlatformAdmin)
        {
            modules = new List<string> { AdminModule.Name };
        }
        else if (!string.IsNullOrEmpty(request.Module))
        {
            if (!modules.Contains(request.Module))
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = $"Su empresa no tiene acceso al m�dulo '{request.Module}'.",
                    code = "MODULE_NOT_ENABLED"
                });
            }
        }

        var token = await _jwtService.GenerateTokenAsync(user);

        // Suscripci�n de la empresa: bloquea el acceso si est� suspendida y expone el estado
        // para que el frontend muestre la etiqueta de vencimiento/pr�rroga.
        object? subscriptionInfo = null;
        if (!isPlatformAdmin && !string.IsNullOrEmpty(user.TenantId))
        {
            var subscription = await _identityContext.TenantSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == user.TenantId);

            if (subscription is not null)
            {
                var nowUtc = DateTime.UtcNow;
                var subStatus = subscription.EvaluateStatus(nowUtc);

                if (subStatus == SubscriptionStatus.Suspended)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "La suscripci�n de tu empresa est� suspendida por falta de pago. Contacta al administrador para reactivar el servicio.",
                        code = "SUBSCRIPTION_SUSPENDED"
                    });
                }

                subscriptionInfo = BuildSubscriptionInfo(subscription, subStatus, nowUtc);
            }
        }

        // El permiso de ocultar totales solo aplica a usuarios del bazar (BazarUser).
        var effectiveCanViewTotals = user.Role == "BazarUser" ? user.CanViewTotals : true;

        var data = new
        {
            Token = token,
            UserId = user.Id,
            user.Email,
            user.Role,
            user.FirstName,
            user.LastName,
            user.SellerId,
            user.TenantId,
            user.IsActive,
            user.MustChangePassword,
            CanViewTotals = effectiveCanViewTotals,
            AllowedModules = SplitModules(user.AllowedModules),
            Modules = modules,
            BackendVersion = _backendVersion,
            Subscription = subscriptionInfo
        };
        return Ok(data);
    }

    /// <summary>
    /// Devuelve el estado actual de la suscripci�n del tenant autenticado.
    /// Se usa para refrescar avisos de vencimiento sin cerrar sesi�n.
    /// </summary>
    [Authorize]
    [HttpGet("subscription-status")]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        var tenantId = _currentUser.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
            return Ok(new { success = true, data = (object?)null });

        var subscription = await _identityContext.TenantSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (subscription is null)
            return Ok(new { success = true, data = (object?)null });

        var nowUtc = DateTime.UtcNow;
        var status = subscription.EvaluateStatus(nowUtc);

        // Si lleg� aqu� estando suspendida, se devuelve el estado para UI; el bloqueo real
        // de acceso ocurre al iniciar sesi�n.
        var data = BuildSubscriptionInfo(subscription, status, nowUtc);
        return Ok(new { success = true, data });
    }

    private static object BuildSubscriptionInfo(
        TenantSubscription subscription,
        SubscriptionStatus status,
        DateTime nowUtc)
    {
        return new
        {
            status = status.ToString(),
            paidUntil = subscription.PaidUntil,
            graceEndsOn = subscription.GraceEndsOn,
            daysUntilExpiration = subscription.DaysUntilExpiration(nowUtc),
            isInGrace = status == SubscriptionStatus.Grace
        };
    }

    /// <summary>
    /// Solicitud p�blica desde el login: contratar o reactivar una cuenta. Guarda la solicitud
    /// y avisa por WhatsApp al super administrador. No requiere autenticaci�n.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("contact-request")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ContactRequest([FromBody] ContactRequestBody body)
    {
        const string defaultSuperAdminPhone = "3121232192";

        var phone = new string((body.Phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (phone.Length is < 10 or > 15)
            return BadRequest(new { success = false, message = "El n�mero debe tener entre 10 y 15 d�gitos." });

        var type = body.Type == "Reactivate" ? "Reactivate" : "Contract";

        _identityContext.ContactRequests.Add(new Domain.Common.Entities.ContactRequest
        {
            Phone = phone,
            Type = type,
            Message = body.Message?.Trim(),
            Status = Domain.Common.Entities.RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        });
        await _identityContext.SaveChangesAsync();

        var superAdminPhone = (await _identityContext.PlatformSettings
            .AsNoTracking()
            .Select(s => s.SuperAdminPhone)
            .FirstOrDefaultAsync()) ?? defaultSuperAdminPhone;

        var label = type == "Reactivate" ? "Reactivar cuenta" : "Contratar cuenta";
        var waMessage =
            $"?? Nueva solicitud desde el login\n" +
            $"Tipo: {label}\n" +
            $"Tel�fono: {phone}\n" +
            (string.IsNullOrWhiteSpace(body.Message) ? "" : $"Mensaje: {body.Message}\n") +
            "Revisa las solicitudes en el panel de administraci�n.";

        try
        {
            await _whatsApp.SendTextAsync(superAdminPhone, waMessage);
        }
        catch
        {
            // Best-effort: la solicitud ya qued� registrada.
        }

        return Ok(new { success = true, message = "Solicitud enviada. Te contactaremos pronto." });
    }

    public class ContactRequestBody
    {
        public string Phone { get; set; } = null!;
        public string Type { get; set; } = "Contract";
        public string? Message { get; set; }
    }

    /// <summary>
    /// Crear usuario comisionista vinculado a un Seller existente.
    /// FirstName/LastName se copian autom�ticamente del Seller.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("commissionists")]
    public async Task<IActionResult> CreateCommissionist([FromBody] CreateCommissionistRequest request)
    {
        var tenantId = _currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return Unauthorized(new { success = false, message = "No se pudo determinar la empresa." });

        // 1. Validar que el Seller exista en el tenant actual
        var seller = await _paymentsDb.Sellers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SellerId);

        if (seller is null)
            return BadRequest(new { success = false, message = "El vendedor (SellerId) no existe en su empresa." });

        // 2. Validar email no duplicado
        var emailExists = await _userManager.FindByEmailAsync(request.Email);
        if (emailExists is not null)
            return BadRequest(new { success = false, message = "El email ya est� registrado." });

        // 3. Validar que no haya otro comisionista con ese SellerId
        var duplicateSeller = await _userManager.Users
            .AnyAsync(u => u.TenantId == tenantId && u.SellerId == request.SellerId);

        if (duplicateSeller)
            return Conflict(new { success = false, message = "Ya existe un usuario comisionista para ese vendedor." });

        // 4. Crear usuario copiando FirstName/LastName del Seller
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = seller.Name,
            LastName = seller.LastName,
            TenantId = tenantId,
            Role = "Commissionist",
            SellerId = request.SellerId,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetCommissionists), null, new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.SellerId,
            user.IsActive
        });
    }

    /// <summary>
    /// Listar todos los comisionistas del tenant.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet("commissionists")]
    public async Task<IActionResult> GetCommissionists()
    {
        var tenantId = _currentUser.TenantId;

        var commissionists = await _userManager.Users
            .Where(u => u.TenantId == tenantId && u.Role == "Commissionist")
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.SellerId,
                u.IsActive
            })
            .ToListAsync();

        return Ok(commissionists);
    }

    /// <summary>
    /// Activar/desactivar usuario comisionista.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("commissionists/{id}/status")]
    public async Task<IActionResult> UpdateCommissionistStatus(string id, [FromBody] UpdateCommissionistStatusRequest request)
    {
        var tenantId = _currentUser.TenantId;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.Role == "Commissionist");

        if (user is null)
            return NotFound(new { success = false, message = "Comisionista no encontrado." });

        user.IsActive = request.IsActive;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            Message = request.IsActive ? "Comisionista activado." : "Comisionista desactivado.",
            UserId = user.Id,
            user.IsActive
        });
    }

    // ============================================================
    // GESTI�N DE USUARIOS DEL BAZAR (rol "BazarUser")
    // ============================================================

    /// <summary>
    /// Obtener el n�mero de WhatsApp del usuario autenticado (para verificaci�n).
    /// </summary>
    [Authorize]
    [HttpGet("me/phone")]
    public async Task<IActionResult> GetMyPhone()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized(new { success = false, message = "Sesi�n no v�lida." });

        return Ok(new { phoneNumber = me.PhoneNumber });
    }

    /// <summary>
    /// Configurar el n�mero de WhatsApp del usuario autenticado.
    /// El SuperAdmin lo necesita para recibir los c�digos de verificaci�n.
    /// </summary>
    [Authorize]
    [HttpPut("me/phone")]
    public async Task<IActionResult> UpdateMyPhone([FromBody] UpdateMyPhoneRequest request)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized(new { success = false, message = "Sesi�n no v�lida." });

        var digits = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : new string(request.PhoneNumber.Where(char.IsDigit).ToArray());

        if (!string.IsNullOrEmpty(digits) && (digits.Length < 10 || digits.Length > 15))
            return BadRequest(new { success = false, message = "El n�mero debe incluir el c�digo de pa�s (10 a 15 d�gitos)." });

        me.PhoneNumber = digits;
        await _userManager.UpdateAsync(me);

        return Ok(new { success = true, phoneNumber = me.PhoneNumber });
    }

    // -------------------------------------------------------------------------
    // PIN de seguridad del SuperAdmin
    // -------------------------------------------------------------------------

    /// <summary>
    /// Indica si el SuperAdmin tiene configurado un PIN de seguridad.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet("me/security-pin/status")]
    public async Task<IActionResult> GetSecurityPinStatus()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized();

        return Ok(new { configured = !string.IsNullOrEmpty(me.AdminSecurityPinHash) });
    }

    /// <summary>
    /// Configura o cambia el PIN de seguridad del SuperAdmin.
    /// Si ya tiene PIN, se requiere el PIN actual para cambiarlo.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("me/security-pin")]
    public async Task<IActionResult> SetSecurityPin([FromBody] SetSecurityPinRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPin) || request.NewPin.Length < 4 || request.NewPin.Length > 8
            || !request.NewPin.All(char.IsDigit))
            return BadRequest(new { success = false, message = "El PIN debe tener entre 4 y 8 d�gitos num�ricos." });

        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized();

        var (success, error) = await _adminPin.SetPinAsync(me.Id, request.NewPin, request.CurrentPin);
        if (!success)
            return BadRequest(new { success = false, message = error ?? "No se pudo guardar el PIN." });

        return Ok(new { success = true, message = "PIN configurado correctamente." });
    }

    /// <summary>
    /// Env�a un c�digo de verificaci�n por WhatsApp al n�mero del SuperAdmin
    /// antes de autorizar una operaci�n sensible (alta/edici�n/baja/reset).
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("verification/request")]
    public async Task<IActionResult> RequestVerification([FromBody] RequestVerificationRequest request)
    {
        var allowedPurposes = new[] { "user.create", "user.update", "user.status", "user.reset-password", "payment.card.add", "payment.card.update", "payment.card.delete", "customer.block.override", "customer.unblock" };
        if (string.IsNullOrWhiteSpace(request.Purpose) || !allowedPurposes.Contains(request.Purpose))
            return BadRequest(new { success = false, message = "Prop�sito de verificaci�n no v�lido." });

        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized(new { success = false, message = "Sesi�n no v�lida." });

        if (string.IsNullOrWhiteSpace(me.PhoneNumber))
        {
            return BadRequest(new
            {
                success = false,
                message = "Tu usuario no tiene un n�mero de WhatsApp registrado. Config�ralo para recibir el c�digo de verificaci�n.",
                code = "NO_PHONE"
            });
        }

        var (challengeId, code) = _verification.Create(request.Purpose, me.Id, TimeSpan.FromMinutes(10));

        var sendResult = await _whatsApp.SendOtpWithResultAsync(me.PhoneNumber, code);
        var delivered = sendResult.Success;

        // Registrar el mensaje para dar seguimiento a su estatus v�a webhooks de Meta.
        try
        {
            _bazaresDb.WhatsAppMessages.Add(new Domain.Bazares.Entities.BzaWhatsAppMessage
            {
                WaMessageId = sendResult.MessageId,
                ToPhone = new string(me.PhoneNumber.Where(char.IsDigit).ToArray()),
                Purpose = "otp",
                Status = delivered ? "sent" : "failed",
                ErrorCode = int.TryParse(sendResult.ErrorCode, out var ec) ? ec : null,
                ErrorMessage = sendResult.ErrorMessage,
                SentAt = DateTime.UtcNow,
            });
            await _bazaresDb.SaveChangesAsync(default);
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "No se pudo registrar el mensaje de WhatsApp para seguimiento.");
        }

        // En desarrollo, registrar el c�digo para poder probar aunque el env�o no llegue.
        _logger.LogInformation(
            "OTP {Purpose} para {UserId} (tel {Phone}): {Code} | entregado={Delivered}",
            request.Purpose, me.Id, MaskPhone(me.PhoneNumber), code, delivered);

        return Ok(new
        {
            success = true,
            challengeId,
            expiresInSeconds = 600,
            sentTo = MaskPhone(me.PhoneNumber),
            delivered,
            message = delivered
                ? "Te enviamos un c�digo de verificaci�n por WhatsApp."
                : "No se pudo entregar el WhatsApp (revisa la configuraci�n/lista de destinatarios). El c�digo qued� registrado en el servidor."
        });
    }

    /// <summary>
    /// Crear un usuario del bazar con permisos por m�dulo y contrase�a temporal.
    /// El usuario deber� cambiar la contrase�a en su primer inicio de sesi�n.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("users")]
    public async Task<IActionResult> CreateBazarUser([FromBody] CreateBazarUserRequest request)
    {
        var tenantId = _currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return Unauthorized(new { success = false, message = "No se pudo determinar la empresa." });

        var challenge = await ValidateChallengeAsync("user.create", request.ChallengeId, request.VerificationCode, request.AdminPin);
        if (challenge is not null)
            return challenge;

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { success = false, message = "El email es obligatorio." });

        if (string.IsNullOrWhiteSpace(request.TemporaryPassword) || request.TemporaryPassword.Length < 6)
            return BadRequest(new { success = false, message = "La contrase�a temporal debe tener al menos 6 caracteres." });

        var emailExists = await _userManager.FindByEmailAsync(request.Email);
        if (emailExists is not null)
            return BadRequest(new { success = false, message = "El email ya est� registrado." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            TenantId = tenantId,
            Role = "BazarUser",
            IsActive = true,
            MustChangePassword = true,
            PasswordChangedAt = null,
            CanViewTotals = request.CanViewTotals,
            AllowedModules = JoinModules(request.AllowedModules)
        };

        var result = await _userManager.CreateAsync(user, request.TemporaryPassword);

        if (!result.Succeeded)
            return BadRequest(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return CreatedAtAction(nameof(GetBazarUsers), null, MapUser(user));
    }

    /// <summary>
    /// Listar los usuarios del bazar del tenant.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet("users")]
    public async Task<IActionResult> GetBazarUsers()
    {
        var tenantId = _currentUser.TenantId;

        var users = await _userManager.Users
            .Where(u => u.TenantId == tenantId && u.Role == "BazarUser")
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        return Ok(users.Select(MapUser));
    }

    /// <summary>
    /// Actualizar datos y permisos de un usuario del bazar.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateBazarUser(string id, [FromBody] UpdateBazarUserRequest request)
    {
        var tenantId = _currentUser.TenantId;

        var challenge = await ValidateChallengeAsync("user.update", request.ChallengeId, request.VerificationCode, request.AdminPin);
        if (challenge is not null)
            return challenge;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.Role == "BazarUser");

        if (user is null)
            return NotFound(new { success = false, message = "Usuario no encontrado." });

        user.FirstName = request.FirstName?.Trim() ?? user.FirstName;
        user.LastName = request.LastName?.Trim() ?? user.LastName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.AllowedModules = JoinModules(request.AllowedModules);
        user.CanViewTotals = request.CanViewTotals;
        user.IsActive = request.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return Ok(MapUser(user));
    }

    /// <summary>
    /// Activar/deshabilitar (cancelar) un usuario del bazar.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> SetBazarUserStatus(string id, [FromBody] SetUserStatusRequest request)
    {
        var tenantId = _currentUser.TenantId;

        var challenge = await ValidateChallengeAsync("user.status", request.ChallengeId, request.VerificationCode, request.AdminPin);
        if (challenge is not null)
            return challenge;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.Role == "BazarUser");

        if (user is null)
            return NotFound(new { success = false, message = "Usuario no encontrado." });

        user.IsActive = request.IsActive;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            Message = request.IsActive ? "Usuario habilitado." : "Usuario deshabilitado.",
            UserId = user.Id,
            user.IsActive
        });
    }

    /// <summary>
    /// Asignar una nueva contrase�a temporal a un usuario (reset por parte del SuperAdmin).
    /// El usuario deber� cambiarla en su pr�ximo inicio de sesi�n.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(string id, [FromBody] ResetUserPasswordRequest request)
    {
        var tenantId = _currentUser.TenantId;

        if (string.IsNullOrWhiteSpace(request.TemporaryPassword) || request.TemporaryPassword.Length < 6)
            return BadRequest(new { success = false, message = "La contrase�a temporal debe tener al menos 6 caracteres." });

        var challenge = await ValidateChallengeAsync("user.reset-password", request.ChallengeId, request.VerificationCode, request.AdminPin);
        if (challenge is not null)
            return challenge;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.Role == "BazarUser");

        if (user is null)
            return NotFound(new { success = false, message = "Usuario no encontrado." });

        // Reemplazar la contrase�a sin requerir token providers.
        await _userManager.RemovePasswordAsync(user);
        var result = await _userManager.AddPasswordAsync(user, request.TemporaryPassword);

        if (!result.Succeeded)
            return BadRequest(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        user.MustChangePassword = true;
        await _userManager.UpdateAsync(user);

        return Ok(new { success = true, message = "Contrase�a temporal asignada. El usuario deber� cambiarla al iniciar sesi�n." });
    }

    /// <summary>
    /// Inicia el flujo de recuperacion de contrasena para un bazar: identifica la cuenta,
    /// crea una sesion temporal y devuelve el contacto enmascarado que el usuario debe
    /// completar antes de recibir el codigo.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot-password/request")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPasswordRequest([FromBody] RequestPasswordRecoveryRequest request)
    {
        if (!Enum.TryParse<PasswordRecoveryChannel>(request.Channel, true, out var channel))
            channel = PasswordRecoveryChannel.Email;

        var result = await _sender.Send(new RequestPasswordRecoveryCommand(request.Email, channel));
        return Ok(new ApiResponse<RequestPasswordRecoveryResult>
        {
            Success = true,
            Message = channel == PasswordRecoveryChannel.Email
                ? "Confirma tu correo para enviar el codigo de recuperacion."
                : "Confirma tu numero de WhatsApp para mostrar el QR de recuperacion.",
            Data = result
        });
    }

    /// <summary>
    /// Confirma el correo o telefono ingresado por el usuario antes de enviar el codigo.
    /// Para correo, el codigo se envia de inmediato.
    /// Para WhatsApp, se devuelve el QR del mensaje especial que valida origen y contenido.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot-password/confirm-contact")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ConfirmForgotPasswordContact([FromBody] ConfirmPasswordRecoveryContactRequest request)
    {
        var result = await _sender.Send(new ConfirmPasswordRecoveryContactCommand(request.SessionId, request.ContactValue));
        return Ok(new ApiResponse<ConfirmPasswordRecoveryContactResult>
        {
            Success = result.Delivered,
            Message = result.Message,
            Data = result
        });
    }

    /// <summary>
    /// Restablece la contrasena usando el codigo de verificacion enviado por correo
    /// o WhatsApp.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot-password/reset")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPasswordReset([FromBody] ResetPasswordRecoveryRequest request)
    {
        var sessionId = !string.IsNullOrWhiteSpace(request.SessionId)
            ? request.SessionId
            : request.ChallengeId;

        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { success = false, message = "La sesion de recuperacion es requerida." });

        var result = await _sender.Send(new ResetPasswordRecoveryCommand(sessionId, request.VerificationCode, request.NewPassword));
        return Ok(new ApiResponse<ResetPasswordRecoveryResult>
        {
            Success = result.Success,
            Message = result.Message,
            Data = result
        });
    }
    /// <summary>
    /// Cambiar la propia contrase�a (contrase�a actual + nueva).
    /// Sirve tanto para el cambio forzado de la contrase�a temporal como para el
    /// cambio voluntario del usuario. Cualquier usuario autenticado.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { success = false, message = "La nueva contrase�a debe tener al menos 6 caracteres." });

        if (request.CurrentPassword == request.NewPassword)
            return BadRequest(new { success = false, message = "La nueva contrase�a debe ser distinta a la actual." });

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new { success = false, message = "Sesi�n no v�lida." });

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var message = result.Errors.Any(e => e.Code == "PasswordMismatch")
                ? "La contrase�a actual es incorrecta."
                : string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(new { success = false, message });
        }

        // Registrar que ya cambi� la contrase�a temporal.
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Emitir un nuevo token con los claims actualizados.
        var token = await _jwtService.GenerateTokenAsync(user);

        return Ok(new
        {
            success = true,
            message = "Contrase�a actualizada correctamente.",
            token,
            mustChangePassword = false
        });
    }

    // ============================================================
    // GESTI�N DE M�DULOS DEL TENANT
    // ============================================================

    /// <summary>
    /// Obtener los m�dulos habilitados de mi empresa.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet("modules")]
    public async Task<IActionResult> GetModules()
    {
        var tenantId = _currentUser.TenantId;

        var modules = await _identityContext.TenantModules
            .Where(tm => tm.TenantId == tenantId)
            .Select(tm => new
            {
                tm.Module,
                tm.IsActive,
                tm.ActivatedAt,
                tm.DeactivatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            TenantId = tenantId,
            AvailableModules = SystemModules.All,
            Modules = modules
        });
    }

    /// <summary>
    /// Activar o desactivar un m�dulo para mi empresa.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("modules/{moduleName}")]
    public async Task<IActionResult> ToggleModule(string moduleName, [FromBody] ToggleModuleRequest request)
    {
        if (!SystemModules.All.Contains(moduleName))
            return BadRequest(new { success = false, message = $"M�dulo '{moduleName}' no es v�lido. Opciones: {string.Join(", ", SystemModules.All)}" });

        var tenantId = _currentUser.TenantId;

        var existing = await _identityContext.TenantModules
            .FirstOrDefaultAsync(tm => tm.TenantId == tenantId && tm.Module == moduleName);

        if (existing == null)
        {
            // Crear registro si no existe
            _identityContext.TenantModules.Add(new TenantModule
            {
                TenantId = tenantId!,
                Module = moduleName,
                IsActive = request.IsActive,
                ActivatedAt = request.IsActive ? DateTime.UtcNow : default,
                DeactivatedAt = request.IsActive ? null : DateTime.UtcNow
            });
        }
        else
        {
            existing.IsActive = request.IsActive;
            if (request.IsActive)
            {
                existing.ActivatedAt = DateTime.UtcNow;
                existing.DeactivatedAt = null;
            }
            else
            {
                existing.DeactivatedAt = DateTime.UtcNow;
            }
        }

        await _identityContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = request.IsActive
                ? $"M�dulo '{moduleName}' activado. Los usuarios deben re-iniciar sesi�n."
                : $"M�dulo '{moduleName}' desactivado. Los usuarios deben re-iniciar sesi�n.",
            module = moduleName,
            isActive = request.IsActive
        });
    }

    // ============================================================
    // HELPERS
    // ============================================================

    /// <summary>
    /// Valida PIN o c�digo OTP seg�n lo que se proporcione.
    /// Si se env�a adminPin, verifica hash. Si se env�a challengeId+code, verifica OTP.
    /// Devuelve null si es v�lido, o un IActionResult de error.
    /// </summary>
    private async Task<IActionResult?> ValidateChallengeAsync(string purpose, string? challengeId, string? code, string? adminPin = null)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized(new { success = false, message = "Sesi�n no v�lida." });

        if (!string.IsNullOrWhiteSpace(adminPin))
        {
            var pinOk = await _adminPin.VerifyPinAsync(me.Id, adminPin);
            if (!pinOk)
                return StatusCode(403, new { success = false, message = "PIN incorrecto.", code = "PIN_INVALID" });
            return null;
        }

        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(code))
        {
            return StatusCode(403, new
            {
                success = false,
                message = "Esta operaci�n requiere verificaci�n (PIN o c�digo WhatsApp).",
                code = "VERIFICATION_REQUIRED"
            });
        }

        if (!_verification.Validate(challengeId, code, purpose, me.Id))
        {
            return StatusCode(403, new
            {
                success = false,
                message = "El c�digo de verificaci�n es inv�lido o expir�.",
                code = "VERIFICATION_INVALID"
            });
        }

        return null;
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length <= 4)
            return new string('�', digits.Length);

        return new string('�', digits.Length - 4) + digits[^4..];
    }

    private static string? JoinModules(string[]? modules)
    {
        if (modules is null || modules.Length == 0)
            return null;

        var cleaned = modules
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var joined = string.Join(",", cleaned);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }

    private static string[] SplitModules(string? modules)
    {
        if (string.IsNullOrWhiteSpace(modules))
            return Array.Empty<string>();

        return modules
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static object MapUser(ApplicationUser user) => new
    {
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        PhoneNumber = user.PhoneNumber,
        user.Role,
        user.IsActive,
        user.MustChangePassword,
        user.PasswordChangedAt,
        user.CanViewTotals,
        AllowedModules = SplitModules(user.AllowedModules)
    };
}

public class ToggleModuleRequest
{
    public bool IsActive { get; set; }
}




