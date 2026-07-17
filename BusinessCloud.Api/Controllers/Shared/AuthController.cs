using BusinessCloud.Application.Auth.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Infrastructure.Common.Services;
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
        ILogger<AuthController> logger)
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

            // Activar módulos solicitados (o todos por defecto)
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
                // Rollback manual: eliminar tenant y módulos creados
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
                Message = "Empresa y Usuario creados con éxito",
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
            return Unauthorized(new { success = false, message = "Credenciales inválidas." });

        if (!user.IsActive)
            return Unauthorized(new { success = false, message = "Usuario desactivado. Contacte al administrador." });

        if (user.Role == "Commissionist" && !user.SellerId.HasValue)
            return BadRequest(new { success = false, message = "Comisionista sin vendedor asignado. Contacte al administrador." });

        // lockoutOnFailure: true -> cuenta los intentos fallidos y bloquea temporalmente (anti fuerza bruta)
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return StatusCode(423, new { success = false, message = "Cuenta bloqueada temporalmente por múltiples intentos fallidos. Intenta de nuevo en unos minutos." });

        if (!result.Succeeded)
            return Unauthorized(new { success = false, message = "Credenciales inválidas." });

        // Obtener módulos habilitados del tenant
        var modules = await _identityContext.TenantModules
            .Where(tm => tm.TenantId == user.TenantId && tm.IsActive)
            .Select(tm => tm.Module)
            .ToListAsync();

        var isPlatformAdmin = user.Role == SystemRoles.PlatformAdmin;

        // El PlatformAdmin es el administrador global del SaaS (cross-tenant): no pertenece a
        // ninguna empresa ni valida módulos de tenant; opera exclusivamente el panel Admin.
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
                    message = $"Su empresa no tiene acceso al módulo '{request.Module}'.",
                    code = "MODULE_NOT_ENABLED"
                });
            }
        }

        var token = await _jwtService.GenerateTokenAsync(user);

        // Suscripción de la empresa: bloquea el acceso si está suspendida y expone el estado
        // para que el frontend muestre la etiqueta de vencimiento/prórroga.
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
                        message = "La suscripción de tu empresa está suspendida por falta de pago. Contacta al administrador para reactivar el servicio.",
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
            Subscription = subscriptionInfo
        };
        return Ok(data);
    }

    /// <summary>
    /// Devuelve el estado actual de la suscripción del tenant autenticado.
    /// Se usa para refrescar avisos de vencimiento sin cerrar sesión.
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

        // Si llegó aquí estando suspendida, se devuelve el estado para UI; el bloqueo real
        // de acceso ocurre al iniciar sesión.
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
    /// Solicitud pública desde el login: contratar o reactivar una cuenta. Guarda la solicitud
    /// y avisa por WhatsApp al super administrador. No requiere autenticación.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("contact-request")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ContactRequest([FromBody] ContactRequestBody body)
    {
        const string defaultSuperAdminPhone = "3121232192";

        var phone = new string((body.Phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (phone.Length is < 10 or > 15)
            return BadRequest(new { success = false, message = "El número debe tener entre 10 y 15 dígitos." });

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
            $"📲 Nueva solicitud desde el login\n" +
            $"Tipo: {label}\n" +
            $"Teléfono: {phone}\n" +
            (string.IsNullOrWhiteSpace(body.Message) ? "" : $"Mensaje: {body.Message}\n") +
            "Revisa las solicitudes en el panel de administración.";

        try
        {
            await _whatsApp.SendTextAsync(superAdminPhone, waMessage);
        }
        catch
        {
            // Best-effort: la solicitud ya quedó registrada.
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
    /// FirstName/LastName se copian automáticamente del Seller.
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
            return BadRequest(new { success = false, message = "El email ya está registrado." });

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
    // GESTIÓN DE USUARIOS DEL BAZAR (rol "BazarUser")
    // ============================================================

    /// <summary>
    /// Obtener el número de WhatsApp del usuario autenticado (para verificación).
    /// </summary>
    [Authorize]
    [HttpGet("me/phone")]
    public async Task<IActionResult> GetMyPhone()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized(new { success = false, message = "Sesión no válida." });

        return Ok(new { phoneNumber = me.PhoneNumber });
    }

    /// <summary>
    /// Configurar el número de WhatsApp del usuario autenticado.
    /// El SuperAdmin lo necesita para recibir los códigos de verificación.
    /// </summary>
    [Authorize]
    [HttpPut("me/phone")]
    public async Task<IActionResult> UpdateMyPhone([FromBody] UpdateMyPhoneRequest request)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized(new { success = false, message = "Sesión no válida." });

        var digits = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : new string(request.PhoneNumber.Where(char.IsDigit).ToArray());

        if (!string.IsNullOrEmpty(digits) && (digits.Length < 10 || digits.Length > 15))
            return BadRequest(new { success = false, message = "El número debe incluir el código de país (10 a 15 dígitos)." });

        me.PhoneNumber = digits;
        await _userManager.UpdateAsync(me);

        return Ok(new { success = true, phoneNumber = me.PhoneNumber });
    }

    /// <summary>
    /// Envía un código de verificación por WhatsApp al número del SuperAdmin
    /// antes de autorizar una operación sensible (alta/edición/baja/reset).
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("verification/request")]
    public async Task<IActionResult> RequestVerification([FromBody] RequestVerificationRequest request)
    {
        var allowedPurposes = new[] { "user.create", "user.update", "user.status", "user.reset-password", "payment.card.add", "payment.card.update", "payment.card.delete", "customer.block.override", "customer.unblock" };
        if (string.IsNullOrWhiteSpace(request.Purpose) || !allowedPurposes.Contains(request.Purpose))
            return BadRequest(new { success = false, message = "Propósito de verificación no válido." });

        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized(new { success = false, message = "Sesión no válida." });

        if (string.IsNullOrWhiteSpace(me.PhoneNumber))
        {
            return BadRequest(new
            {
                success = false,
                message = "Tu usuario no tiene un número de WhatsApp registrado. Configúralo para recibir el código de verificación.",
                code = "NO_PHONE"
            });
        }

        var (challengeId, code) = _verification.Create(request.Purpose, me.Id, TimeSpan.FromMinutes(10));

        var sendResult = await _whatsApp.SendOtpWithResultAsync(me.PhoneNumber, code);
        var delivered = sendResult.Success;

        // Registrar el mensaje para dar seguimiento a su estatus vía webhooks de Meta.
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

        // En desarrollo, registrar el código para poder probar aunque el envío no llegue.
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
                ? "Te enviamos un código de verificación por WhatsApp."
                : "No se pudo entregar el WhatsApp (revisa la configuración/lista de destinatarios). El código quedó registrado en el servidor."
        });
    }

    /// <summary>
    /// Crear un usuario del bazar con permisos por módulo y contraseña temporal.
    /// El usuario deberá cambiar la contraseña en su primer inicio de sesión.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("users")]
    public async Task<IActionResult> CreateBazarUser([FromBody] CreateBazarUserRequest request)
    {
        var tenantId = _currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return Unauthorized(new { success = false, message = "No se pudo determinar la empresa." });

        var challenge = await ValidateChallengeAsync("user.create", request.ChallengeId, request.VerificationCode);
        if (challenge is not null)
            return challenge;

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { success = false, message = "El email es obligatorio." });

        if (string.IsNullOrWhiteSpace(request.TemporaryPassword) || request.TemporaryPassword.Length < 6)
            return BadRequest(new { success = false, message = "La contraseña temporal debe tener al menos 6 caracteres." });

        var emailExists = await _userManager.FindByEmailAsync(request.Email);
        if (emailExists is not null)
            return BadRequest(new { success = false, message = "El email ya está registrado." });

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

        var challenge = await ValidateChallengeAsync("user.update", request.ChallengeId, request.VerificationCode);
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

        var challenge = await ValidateChallengeAsync("user.status", request.ChallengeId, request.VerificationCode);
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
    /// Asignar una nueva contraseña temporal a un usuario (reset por parte del SuperAdmin).
    /// El usuario deberá cambiarla en su próximo inicio de sesión.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(string id, [FromBody] ResetUserPasswordRequest request)
    {
        var tenantId = _currentUser.TenantId;

        if (string.IsNullOrWhiteSpace(request.TemporaryPassword) || request.TemporaryPassword.Length < 6)
            return BadRequest(new { success = false, message = "La contraseña temporal debe tener al menos 6 caracteres." });

        var challenge = await ValidateChallengeAsync("user.reset-password", request.ChallengeId, request.VerificationCode);
        if (challenge is not null)
            return challenge;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.Role == "BazarUser");

        if (user is null)
            return NotFound(new { success = false, message = "Usuario no encontrado." });

        // Reemplazar la contraseña sin requerir token providers.
        await _userManager.RemovePasswordAsync(user);
        var result = await _userManager.AddPasswordAsync(user, request.TemporaryPassword);

        if (!result.Succeeded)
            return BadRequest(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        user.MustChangePassword = true;
        await _userManager.UpdateAsync(user);

        return Ok(new { success = true, message = "Contraseña temporal asignada. El usuario deberá cambiarla al iniciar sesión." });
    }

    /// <summary>
    /// Cambiar la propia contraseña (contraseña actual + nueva).
    /// Sirve tanto para el cambio forzado de la contraseña temporal como para el
    /// cambio voluntario del usuario. Cualquier usuario autenticado.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { success = false, message = "La nueva contraseña debe tener al menos 6 caracteres." });

        if (request.CurrentPassword == request.NewPassword)
            return BadRequest(new { success = false, message = "La nueva contraseña debe ser distinta a la actual." });

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new { success = false, message = "Sesión no válida." });

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var message = result.Errors.Any(e => e.Code == "PasswordMismatch")
                ? "La contraseña actual es incorrecta."
                : string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(new { success = false, message });
        }

        // Registrar que ya cambió la contraseña temporal.
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Emitir un nuevo token con los claims actualizados.
        var token = await _jwtService.GenerateTokenAsync(user);

        return Ok(new
        {
            success = true,
            message = "Contraseña actualizada correctamente.",
            token,
            mustChangePassword = false
        });
    }

    // ============================================================
    // GESTIÓN DE MÓDULOS DEL TENANT
    // ============================================================

    /// <summary>
    /// Obtener los módulos habilitados de mi empresa.
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
    /// Activar o desactivar un módulo para mi empresa.
    /// Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("modules/{moduleName}")]
    public async Task<IActionResult> ToggleModule(string moduleName, [FromBody] ToggleModuleRequest request)
    {
        if (!SystemModules.All.Contains(moduleName))
            return BadRequest(new { success = false, message = $"Módulo '{moduleName}' no es válido. Opciones: {string.Join(", ", SystemModules.All)}" });

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
                ? $"Módulo '{moduleName}' activado. Los usuarios deben re-iniciar sesión."
                : $"Módulo '{moduleName}' desactivado. Los usuarios deben re-iniciar sesión.",
            module = moduleName,
            isActive = request.IsActive
        });
    }

    // ============================================================
    // HELPERS
    // ============================================================

    /// <summary>
    /// Valida el código OTP del desafío para el propósito indicado.
    /// Devuelve null si es válido, o un IActionResult de error si no lo es.
    /// </summary>
    private async Task<IActionResult?> ValidateChallengeAsync(string purpose, string? challengeId, string? code)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null)
            return Unauthorized(new { success = false, message = "Sesión no válida." });

        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(code))
        {
            return StatusCode(403, new
            {
                success = false,
                message = "Esta operación requiere verificación por WhatsApp.",
                code = "VERIFICATION_REQUIRED"
            });
        }

        if (!_verification.Validate(challengeId, code, purpose, me.Id))
        {
            return StatusCode(403, new
            {
                success = false,
                message = "El código de verificación es inválido o expiró.",
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
            return new string('•', digits.Length);

        return new string('•', digits.Length - 4) + digits[^4..];
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