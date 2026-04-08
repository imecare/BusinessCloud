using BusinessCloud.Api.Middleware;
using BusinessCloud.Application;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using BusinessCloud.Infrastructure.Common.Services;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

// Evita que ASP.NET Core cambie los nombres de los claims
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// --- CONFIGURACIÓN DE SERILOG TEMPRANA ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/BusinessCloud-Startup.txt", rollingInterval: RollingInterval.Day)
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

    builder.Services.AddScoped<IPaymentsDbContext>(provider =>
        provider.GetRequiredService<PaymentsDbContext>());

    // MediatR (Solo un registro)
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssemblies(
            typeof(Program).Assembly,
            typeof(BusinessCloud.Application.Payments.Commands.CreateCustomer.CreateCustomerHandler).Assembly
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

    // MongoDB
    var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
        ?? builder.Configuration["ConnectionStrings:MongoDb"]
        ?? builder.Configuration["MongoDb"];

    if (string.IsNullOrWhiteSpace(mongoConnectionString))
    {
        throw new InvalidOperationException("La cadena de conexión 'MongoDb' no está configurada en appsettings.json.");
    }

    builder.Services.AddSingleton<MongoDB.Driver.IMongoClient>(sp => new MongoDB.Driver.MongoClient(mongoConnectionString));
    builder.Services.AddScoped<IMongoContext, MongoContext>();
    
    // Application y Controllers
    builder.Services.AddApplication();
    builder.Services.AddControllers();

    // --- CONFIGURACIÓN JWT ---
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key", "La clave JWT no está configurada.");
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

    var app = builder.Build();

    // --- 2. Middleware ---
    app.UseMiddleware<ExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fallo grave durante el arranque de la aplicación (Application Startup Failed)");
}
finally
{
    Log.CloseAndFlush();
}