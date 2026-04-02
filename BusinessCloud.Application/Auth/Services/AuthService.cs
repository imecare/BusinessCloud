using BusinessCloud.Domain.Commissions.Entities;
using BusinessCloud.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BusinessCloud.Application.Auth.Interfaces;

public class AuthService : IAuthService
{
    private readonly CommissionsDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<InfluenceCenter> _hasher = new();

    public AuthService(
        CommissionsDbContext db,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new ArgumentException("Username es requerido.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password es requerido.");

        var user = await _db.InfluenceCenters
            .FirstOrDefaultAsync(x => x.Username == request.Username);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Usuario o contraseña inválidos.");

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            throw new UnauthorizedAccessException("Usuario sin contraseña configurada.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Usuario o contraseña inválidos.");

        var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"]!);

        return new LoginResponse
        {
            Token = _jwtTokenService.GenerateToken(user),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes),
            UserId = user.Id,
            Username = user.Username!,
            Role = user.Role,
            Name = user.Name
        };
    }
}
