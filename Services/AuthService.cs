using MarksApi.Data;
using MarksApi.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user is null)
        {
            _logger.LogWarning("Login failed: unknown username {Username}", request.Username);
            return null;
        }
        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: wrong password for {Username}", request.Username);
            return null;
        }

        var claims = new List<Claim>{
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
    
    

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"]!));

        var token = new JwtSecurityToken(
            
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new LoginResponse { Token = tokenString, ExpiresAtUtc = expires, Role = user.Role };
    }
}