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
using MediatR;
using System.IdentityModel.Tokens.Jwt;

// Evita que ASP.NET Core cambie los nombres de los claims
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();


var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE SERILOG (Debe ir antes de builder.Build) ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/BusinessCloud-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// --- 1. Servicios (DI) ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddEndpointsApiExplorer();

// REGISTRO DE SWAGGER (SOLO UNA VEZ)
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
    options.Password.RequireDigit = false; // Configura según tu necesidad
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<IdentityDbContext>();

// Registra MediatR buscando todos los Handlers en el proyecto de Application
builder.Services.AddScoped<IPaymentsDbContext>(provider =>
    provider.GetRequiredService<PaymentsDbContext>());

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<BusinessCloud.Application.Payments.Commands.CreateCustomer.CreateCustomerHandler>()
);

// O simplemente, si quieres registrar todos los handlers de tu solución:
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(BusinessCloud.Application.Payments.Commands.CreateCustomer.CreateCustomerHandler).Assembly
    )
);

// Configuración de Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
                            ?? "localhost:6379"; // Valor por defecto si no lo encuentra
    options.InstanceName = "BusinessCloud_";
});

// Registrar TU interfaz de caché
builder.Services.AddScoped<ICacheService, RedisCacheService>();




builder.Services.AddScoped<JwtTokenService>();

// Obtener y validar la cadena de conexión de MongoDB (intenta varias ubicaciones)
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
    ?? builder.Configuration["ConnectionStrings:MongoDb"]
    ?? builder.Configuration["MongoDb"];

if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    throw new InvalidOperationException("La cadena de conexión 'MongoDb' no está configurada. Añádela en ConnectionStrings:MongoDb en appsettings.json, User Secrets o variables de entorno.");
}

// Registrar IMongoClient para que MongoContext pueda construirse
builder.Services.AddSingleton<MongoDB.Driver.IMongoClient>(sp => new MongoDB.Driver.MongoClient(mongoConnectionString));

// Registrar el contexto que depende de IMongoClient
builder.Services.AddScoped<IMongoContext, MongoContext>();
builder.Services.AddApplication();
builder.Services.AddControllers();

// --- CONFIGURACIÓN JWT ---
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

var app = builder.Build();

// --- 2. Middleware ---
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Para ver detalles de errores 500
    
    // ESTAS DOS LÍNEAS SON LAS QUE FALTAN PARA QUE CARGUE SWAGGER
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // 1. Extrae y valida el JWT
app.UseAuthorization();  // 2. Valida los permisos

app.MapControllers();

app.Run();