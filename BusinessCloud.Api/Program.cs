using BusinessCloud.Api.Middleware;
using BusinessCloud.Application;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Common.Services;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Infrastructure.Services;
using Microsoft.ApplicationInsights.Extensibility;
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

// --- CONFIGURACIÓN DE SERILOG TEMPRANA ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
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
    builder.Services.AddDbContext<CommissionsDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("CommissionsConnection")));

    builder.Services.AddDbContext<PaymentsDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsConnection")));

    builder.Services.AddDbContext<IdentityDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsConnection")));

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<IdentityDbContext>();

    // Registro del Contexto de Bazares (SQL Server)
    builder.Services.AddDbContext<BazaresDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsConnection"))); // Usa la misma conexión si están en la misma BD

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

    // Configuración de Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        options.InstanceName = "BusinessCloud_";
    });

    builder.Services.AddScoped<ICacheService, RedisCacheService>();
    builder.Services.AddScoped<JwtTokenService>();

    var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");

    // Solo registrar Mongo si existe la cadena, para que no truene en Azure si no lo usas
    if (!string.IsNullOrWhiteSpace(mongoConnectionString))
    {
        builder.Services.AddSingleton<MongoDB.Driver.IMongoClient>(sp => new MongoDB.Driver.MongoClient(mongoConnectionString));
        builder.Services.AddScoped<IMongoContext, MongoContext>();
    }
    else
    {
        Log.Warning("MongoDb no configurado. Algunas funciones podrían no estar disponibles.");
    }

    // Application y Controllers
    builder.Services.AddApplication();
    builder.Services.AddControllers();

    // Rate Limiting para endpoints públicos
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("public-history", opt =>
        {
            opt.PermitLimit = 10;          // máximo 10 requests
            opt.Window = TimeSpan.FromMinutes(1); // por minuto
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // --- CONFIGURACIÓN JWT ---
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("La clave JWT no está configurada en 'Jwt:Key'.");
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
            IssuerSigningKey = new SymmetricSecurityKey(key)
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
    //if (app.Environment.IsDevelopment())
    //{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BusinessCloud API v1");
        c.RoutePrefix = string.Empty; // Esto hace que Swagger salga en la raíz de la URL
    });
    //}
    app.UseCors(builder =>
    builder
       .WithOrigins("http://localhost:5173", "https://bcloud.com.mx", "https://stapp-bcloud-payments.azurestaticapps.net", "https://jolly-sky-02a5e1c10.7.azurestaticapps.net")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
);
    // REGISTRA TU MIDDLEWARE AQUÍ PARA QUE sea EL QUE DICTA EL FORMATO
    app.UseMiddleware<ExceptionMiddleware>();

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fallo grave durante el arranque de la aplicación (Application Startup Failed)");
}
finally
{
    await Log.CloseAndFlushAsync();
}