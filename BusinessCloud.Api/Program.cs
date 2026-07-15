using BusinessCloud.Api.Middleware;
using BusinessCloud.Application;
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

// --- CONFIGURACI�N DE SERILOG TEMPRANA ---
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

        // Evita revelar si un email existe y exige emails únicos
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

    // MediatR (Solo un registro)
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssemblies(
            typeof(Program).Assembly,
            typeof(BusinessCloud.Application.Payments.Commands.CreateSeller.CreateSellerHandler).Assembly
        )
    );

    // Configuraci�n de Redis (opcional)
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
        builder.Services.AddScoped<ICacheService, NoOpCacheService>();
        Log.Warning("Redis no configurado. Usando cach� en memoria (no-op).");
    }

    builder.Services.AddScoped<JwtTokenService>();

    // WhatsApp Cloud API (Meta) + verificación OTP para operaciones sensibles de usuarios
    builder.Services.Configure<BusinessCloud.Infrastructure.Common.Options.WhatsAppOptions>(
        builder.Configuration.GetSection(BusinessCloud.Infrastructure.Common.Options.WhatsAppOptions.SectionName));
    builder.Services.AddHttpClient<BusinessCloud.Application.Common.Interfaces.IWhatsAppSender,
        BusinessCloud.Infrastructure.Common.Services.WhatsAppSender>();
    builder.Services.AddSingleton<BusinessCloud.Application.Common.Interfaces.IVerificationCodeService,
        BusinessCloud.Infrastructure.Common.Services.VerificationCodeService>();

    // Configuraci�n de MongoDB (opcional)
    var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
    if (!string.IsNullOrWhiteSpace(mongoConnectionString) && !mongoConnectionString.Contains("localhost"))
    {
        builder.Services.AddSingleton<MongoDB.Driver.IMongoClient>(sp => new MongoDB.Driver.MongoClient(mongoConnectionString));
        builder.Services.AddScoped<IMongoContext, MongoContext>();
        Log.Information("MongoDB configurado correctamente.");
    }
    else
    {
        builder.Services.AddScoped<IMongoContext, NoOpMongoContext>();
        Log.Warning("MongoDB no configurado. Funciones de auditoría e historial deshabilitadas.");
    }

    // Configuración de Azure Blob Storage
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
        Log.Warning("Azure Blob Storage no configurado. Usando implementación no-op (subida de archivos deshabilitada).");
    }

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5136",
                    "http://localhost:4200",
                    "http://localhost:53517",
                    "http://localhost:4200",
                    "https://bcloud.com.mx",
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

    // Rate Limiting para endpoints p�blicos
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("public-history", opt =>
        {
            opt.PermitLimit = 10;          // m�ximo 10 requests
            opt.Window = TimeSpan.FromMinutes(1); // por minuto
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
        });

        // Anti fuerza bruta en autenticaci�n: l�mite por IP en login/registro.
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

    // --- CONFIGURACI�N JWT ---
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
        throw new InvalidOperationException("La clave JWT no está configurada en 'Jwt:Key'. Configúrala vía user-secrets (desarrollo) o variable de entorno 'Jwt__Key' (producción).");
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

    // Swagger solo en desarrollo (no exponer la superficie de la API en producción)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "BusinessCloud API v1");
            c.RoutePrefix = string.Empty; // Esto hace que Swagger salga en la raíz de la URL
        });
    }

    app.UseCors("AllowFrontend");
    // REGISTRA TU MIDDLEWARE AQU� PARA QUE sea EL QUE DICTA EL FORMATO
    app.UseMiddleware<ExceptionMiddleware>();

    // Sirve los archivos subidos localmente (comprobantes, logos) en la ruta /uploads.
    // Solo tiene efecto cuando el almacenamiento local está habilitado; la carpeta se
    // crea siempre para evitar errores si aún no existe.
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
    app.MapControllers();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fallo grave durante el arranque de la aplicaci�n (Application Startup Failed)");

    // Crear una app m�nima que muestre el error para diagn�stico en Azure
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