using BusinessCloud.Api.Middleware;
using BusinessCloud.Application;
using BusinessCloud.Api.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Common.Services;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;

// Evita que ASP.NET Core cambie los nombres de los claims
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// --- CONFIGURACIï¿½N DE SERILOG TEMPRANA ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Iniciando la API de BusinessCloud...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // --- 1. Servicios (DI) ---
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddEndpointsApiExplorer();

    // REGISTRO DE SWAGGER
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "BusinessCloud API", Version = "v1" });
        // Evita colisiones de schemaId cuando existen DTOs con el mismo nombre
        // en distintos espacios de nombres (p. ej. ImportCollectorDto).
        c.CustomSchemaIds(t => t.FullName);
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: 'Bearer 12345abcdef'",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Bases de Datos
    var commissionsConn = builder.Configuration.GetConnectionString("CommissionsConnection")
        ?? builder.Configuration.GetConnectionString("PaymentsConnection");
    builder.Services.AddDbContext<CommissionsDbContext>(options =>
        options.UseSqlServer(commissionsConn, sql => sql.EnableRetryOnFailure()));

    builder.Services.AddDbContext<PaymentsDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsConnection"), sql => sql.EnableRetryOnFailure()));

    builder.Services.AddDbContext<IdentityDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsConnection"), sql => sql.EnableRetryOnFailure()));

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;

        // Bloqueo de cuenta ante intentos fallidos (mitiga fuerza bruta / robo de credenciales)
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        // Evita revelar si un email existe y exige emails Ãºnicos
        options.User.RequireUniqueEmail = true;
    })
    .AddErrorDescriber<BusinessCloud.Api.Common.SpanishIdentityErrorDescriber>()
    .AddEntityFrameworkStores<IdentityDbContext>();

    // Registro del Contexto de Bazares (SQL Server)
    builder.Services.AddDbContext<BazaresDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsConnection"), sql => sql.EnableRetryOnFailure()));

    // Registro de la Interfaz
    builder.Services.AddScoped<IBazaresDbContext>(provider =>
        provider.GetRequiredService<BazaresDbContext>());

    builder.Services.AddScoped<IPaymentsDbContext>(provider =>
        provider.GetRequiredService<PaymentsDbContext>());

    builder.Services.AddScoped<IIdentityDbContext>(provider =>
        provider.GetRequiredService<IdentityDbContext>());

    // MediatR (Solo un registro)
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssemblies(
            typeof(Program).Assembly,
            typeof(BusinessCloud.Application.Payments.Commands.CreateSeller.CreateSellerHandler).Assembly
        )
    );

    // Configuraciï¿½n de Redis (opcional)
    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redisConnection) && redisConnection != "localhost:6379")
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "BusinessCloud_";
        });
        builder.Services.AddScoped<ICacheService, RedisCacheService>();
        Log.Information("Redis configurado correctamente.");
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddScoped<ICacheService, RedisCacheService>();
        Log.Warning("Redis no configurado. Usando cachÃ© distribuida en memoria.");
    }

    // Cache en memoria del proceso, usado por el throttle del registro de última actividad.
    builder.Services.AddMemoryCache();

    builder.Services.AddScoped<JwtTokenService>();

    // WhatsApp Cloud API (Meta) + verificaciÃ³n OTP para operaciones sensibles de usuarios
    builder.Services.Configure<BusinessCloud.Infrastructure.Common.Options.WhatsAppOptions>(
        builder.Configuration.GetSection(BusinessCloud.Infrastructure.Common.Options.WhatsAppOptions.SectionName));
    builder.Services.Configure<BusinessCloud.Infrastructure.Common.Options.EmailOptions>(
        builder.Configuration.GetSection(BusinessCloud.Infrastructure.Common.Options.EmailOptions.SectionName));
    builder.Services.AddHttpClient<BusinessCloud.Application.Common.Interfaces.IWhatsAppSender,
        BusinessCloud.Infrastructure.Common.Services.WhatsAppSender>();
    builder.Services.AddScoped<BusinessCloud.Application.Common.Interfaces.IWhatsAppNotificationService,
        BusinessCloud.Infrastructure.Common.Services.WhatsAppNotificationService>();
    builder.Services.AddScoped<BusinessCloud.Application.Common.Interfaces.IEmailSender,
        BusinessCloud.Infrastructure.Common.Services.AzureEmailSender>();
    builder.Services.AddSingleton<BusinessCloud.Application.Common.Interfaces.IPasswordRecoverySessionStore,
        BusinessCloud.Infrastructure.Common.Services.PasswordRecoverySessionStore>();
    builder.Services.AddSingleton<IWhatsAppWebhookCommandQueue, WhatsAppWebhookCommandQueue>();
    builder.Services.AddHostedService<WhatsAppWebhookBackgroundService>();
    builder.Services.Configure<BusinessCloud.Infrastructure.Common.Options.WebPushOptions>(
        builder.Configuration.GetSection(BusinessCloud.Infrastructure.Common.Options.WebPushOptions.SectionName));
    builder.Services.AddScoped<BusinessCloud.Application.Common.Interfaces.IWebPushService,
        BusinessCloud.Infrastructure.Common.Services.WebPushService>();
    builder.Services.AddSingleton<BusinessCloud.Application.Common.Interfaces.IVerificationCodeService,
        BusinessCloud.Infrastructure.Common.Services.VerificationCodeService>();

            builder.Services.AddScoped<BusinessCloud.Application.Common.Interfaces.IAdminPinService,
                BusinessCloud.Infrastructure.Common.Services.AdminPinService>();

    // ConfiguraciÃ³n de MongoDB (opcional)
    var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
    if (!string.IsNullOrWhiteSpace(mongoConnectionString) && !mongoConnectionString.Contains("localhost"))
    {
        builder.Services.AddSingleton<MongoDB.Driver.IMongoClient>(sp =>
        {
            // Timeout corto: si el clÃºster de Mongo no responde, que falle rÃ¡pido (no 30s por default)
            // para no bloquear los endpoints que dependen de auditorÃ­a/historial (best-effort).
            var settings = MongoDB.Driver.MongoClientSettings.FromConnectionString(mongoConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout = TimeSpan.FromSeconds(5);
            settings.SocketTimeout = TimeSpan.FromSeconds(5);
            return new MongoDB.Driver.MongoClient(settings);
        });
        builder.Services.AddScoped<IMongoContext, MongoContext>();
        Log.Information("MongoDB configurado correctamente.");
    }
    else
    {
        builder.Services.AddScoped<IMongoContext, NoOpMongoContext>();
        Log.Warning("MongoDB no configurado. Funciones de auditorÃ­a e historial deshabilitadas.");
    }

    // ConfiguraciÃ³n de Azure Blob Storage
    var blobConnectionString = builder.Configuration.GetConnectionString("AzureBlobStorage");
    if (string.Equals(blobConnectionString, "Local", StringComparison.OrdinalIgnoreCase))
    {
        // Modo desarrollo: guarda los archivos en disco y los sirve en /uploads.
        builder.Services.AddScoped<IBlobStorageService, BusinessCloud.Api.Common.LocalFileBlobStorageService>();
        Log.Information("Almacenamiento local de archivos habilitado (uploads en disco, ruta /uploads).");
    }
    else if (!string.IsNullOrWhiteSpace(blobConnectionString))
    {
        builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
        Log.Information("Azure Blob Storage configurado correctamente.");
    }
    else
    {
        builder.Services.AddScoped<IBlobStorageService, NoOpBlobStorageService>();
        Log.Warning("Azure Blob Storage no configurado. Usando implementaciÃ³n no-op (subida de archivos deshabilitada).");
    }

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5136",
                    "http://localhost:4200",
                    "http://localhost:4300",
                    "http://localhost:53517",
                    "https://bcloud.com.mx",
                    "https://admin.bcloud.com.mx",
                    "https://payments.bcloud.com.mx",
                    "https://bazares.bcloud.com.mx/",
                    "https://stapp-bcloud-payments.azurestaticapps.net",
                    "https://jolly-sky-02a51ec10.7.azurestaticapps.net",
                    "https://bazares.bcloud.com.mx",
                    "https://white-dune-081b9a710.7.azurestaticapps.net")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Application y Controllers
    builder.Services.AddApplication();
    builder.Services.AddControllers();

    // Rate Limiting para endpoints pï¿½blicos
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("public-history", opt =>
        {
            opt.PermitLimit = 10;          // mï¿½ximo 10 requests
            opt.Window = TimeSpan.FromMinutes(1); // por minuto
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
        });

        // Anti fuerza bruta en autenticaciï¿½n: lï¿½mite por IP en login/registro.
        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }));

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // --- CONFIGURACIï¿½N JWT ---
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
        throw new InvalidOperationException("La clave JWT no estÃ¡ configurada en 'Jwt:Key'. ConfigÃºrala vÃ­a user-secrets (desarrollo) o variable de entorno 'Jwt__Key' (producciÃ³n).");
    var key = Encoding.UTF8.GetBytes(jwtKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"))
        .AddPolicy("PlatformAdmin", policy => policy.RequireRole("PlatformAdmin"))
        .AddPolicy("Commissionist", policy => policy.RequireRole("Commissionist"))
        .AddPolicy("SuperAdminOrCommissionist", policy =>
            policy.RequireRole("SuperAdmin", "Commissionist"))
        .AddPolicy("Module_Payments", policy =>
            policy.Requirements.Add(new BusinessCloud.Api.Authorization.ModuleRequirement("Payments")))
        .AddPolicy("Module_Bazares", policy =>
            policy.Requirements.Add(new BusinessCloud.Api.Authorization.ModuleRequirement("Bazares")))
        .AddPolicy("Module_Commissions", policy =>
            policy.Requirements.Add(new BusinessCloud.Api.Authorization.ModuleRequirement("Commissions")));

    builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
        BusinessCloud.Api.Authorization.ModuleRequirementHandler>();

    var app = builder.Build();

    // Apply pending Bazares schema changes before serving requests. EF serializes concurrent
    // migration attempts, so scaled-out instances cannot apply the same migration twice.
    await using (var migrationScope = app.Services.CreateAsyncScope())
    {
        var bazaresDb = migrationScope.ServiceProvider.GetRequiredService<BazaresDbContext>();
        await bazaresDb.Database.MigrateAsync();
        Log.Information("Migraciones de Bazares aplicadas correctamente.");

        // PaymentsDbContext comparte la misma base que Bazares (PaymentsConnection) y el mismo
        // historial de migraciones. Aplicamos aquí sus migraciones pendientes para que columnas
        // como Expenses.IsReceived / Expenses.ReceivedAt no queden sin desplegar en producción.
        var paymentsDb = migrationScope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        await paymentsDb.Database.MigrateAsync();
        Log.Information("Migraciones de Payments aplicadas correctamente.");

        // IdentityDbContext comparte la misma base (PaymentsConnection) con su propio historial
        // de migraciones. Se aplican aquí sus migraciones pendientes (p. ej. AspNetUsers.LastActivityAt)
        // para que no queden sin desplegar en producción.
        var identityDb = migrationScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await identityDb.Database.MigrateAsync();
        Log.Information("Migraciones de Identity aplicadas correctamente.");
    }

    // --- 2. Middleware ---

    // Cabeceras de seguridad HTTP (defensa ante clickjacking, MIME sniffing y fuga de referrer)
    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        await next();
    });

    // Swagger solo en desarrollo (no exponer la superficie de la API en producciÃ³n)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "BusinessCloud API v1");
            c.RoutePrefix = string.Empty; // Esto hace que Swagger salga en la raÃ­z de la URL
        });
    }

    app.UseCors("AllowFrontend");
    app.UseMiddleware<WhatsAppWebhookSignatureMiddleware>();
    // REGISTRA TU MIDDLEWARE AQUï¿½ PARA QUE sea EL QUE DICTA EL FORMATO
    app.UseMiddleware<ExceptionMiddleware>();

    // Sirve los archivos subidos localmente (comprobantes, logos) en la ruta /uploads.
    // Solo tiene efecto cuando el almacenamiento local estÃ¡ habilitado; la carpeta se
    // crea siempre para evitar errores si aÃºn no existe.
    var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
    Directory.CreateDirectory(uploadsPath);
    app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<LastActivityMiddleware>();
    app.MapControllers();

    // --- Seeding del administrador global del SaaS (PlatformAdmin) ---
    // Las credenciales se leen de configuraciÃ³n segura (user-secrets / variables de entorno):
    //   PlatformAdmin:Email, PlatformAdmin:Password, PlatformAdmin:FirstName, PlatformAdmin:LastName
    // Nunca se guardan en el control de versiones.
    try
    {
        using var seedScope = app.Services.CreateScope();
        var seedServices = seedScope.ServiceProvider;
        var seedConfig = seedServices.GetRequiredService<IConfiguration>();
        var seedEmail = seedConfig["PlatformAdmin:Email"];
        var seedPassword = seedConfig["PlatformAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(seedEmail) && !string.IsNullOrWhiteSpace(seedPassword))
        {
            var userMgr = seedServices.GetRequiredService<UserManager<ApplicationUser>>();
            var existing = await userMgr.FindByEmailAsync(seedEmail);
            if (existing is null)
            {
                var platformAdmin = new ApplicationUser
                {
                    UserName = seedEmail,
                    Email = seedEmail,
                    FirstName = seedConfig["PlatformAdmin:FirstName"] ?? "Platform",
                    LastName = seedConfig["PlatformAdmin:LastName"] ?? "Admin",
                    TenantId = string.Empty,
                    Role = BusinessCloud.Domain.Common.Entities.SystemRoles.PlatformAdmin,
                    IsActive = true,
                    EmailConfirmed = true
                };
                var seedResult = await userMgr.CreateAsync(platformAdmin, seedPassword);
                if (seedResult.Succeeded)
                    Log.Information("PlatformAdmin sembrado correctamente: {Email}", seedEmail);
                else
                    Log.Warning("No se pudo sembrar el PlatformAdmin: {Errors}",
                        string.Join("; ", seedResult.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception seedEx)
    {
        // El seeding es best-effort: si la base de datos no estÃ¡ disponible (p. ej. Azure SQL
        // en pausa), no debe impedir el arranque de la API. Reintentar al reiniciar.
        Log.Warning(seedEx, "No se pudo sembrar el PlatformAdmin (Â¿base de datos no disponible?). La API continÃºa el arranque.");
    }

    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fallo grave durante el arranque de la aplicaciï¿½n (Application Startup Failed)");

    // Crear una app mï¿½nima que muestre el error para diagnï¿½stico en Azure
    var errorApp = WebApplication.CreateBuilder(args).Build();
    var errorMessage = ex.ToString();
    errorApp.MapGet("/{**path}", () => Results.Text(
        $"ERROR AL ARRANCAR LA API:\n\n{errorMessage}",
        "text/plain", statusCode: 500));
    await errorApp.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
