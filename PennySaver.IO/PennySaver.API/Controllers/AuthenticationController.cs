using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PennySaver.API.Models;
using Microsoft.EntityFrameworkCore;
using PennySaver.API.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PennySaver.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IDbContextFactory<PennySaverDbContext> dbContextFactory, IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly IDbContextFactory<PennySaverDbContext> _dbContextFactory = dbContextFactory;
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    [AllowAnonymous]
    [HttpPost("token")]
    public IActionResult IssueToken([FromBody] LoginRequest request)
    {
        User? user = null;

        using (var dbContext = _dbContextFactory.CreateDbContext())
        {
            user = dbContext.Users
                .FirstOrDefault(u => u.Email == request.Email);
        }

        if (user == null)
            return Unauthorized(new { message = "Invalid username or password." });

        var passwordHasher = new PasswordHasher<User>();
        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid username or password." });

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var keyBytes = Encoding.UTF8.GetBytes(_jwtOptions.Key);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var newRefreshToken = new RefreshToken
        {
            Token = GenerateSecureRefreshToken(),
            Expires = DateTime.UtcNow.AddDays(7),
            UserId = user.UserId
        };

        using (var dbContext = _dbContextFactory.CreateDbContext())
        {
            dbContext.RefreshTokens.Add(newRefreshToken);
            dbContext.SaveChanges();
        }

        return Ok(new 
        {
             token = tokenString,
             refresh_token = newRefreshToken.Token,
             Token_type = "Bearer",
             expires_utc = expires
        });
    }

    public record TokenRefreshDto(string RefreshToken);

    [AllowAnonymous]
    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] TokenRefreshDto request)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        
        RefreshToken? storedToken = dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefault(rt => rt.Token == request.RefreshToken);

        if (storedToken == null || !storedToken.IsActive)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        storedToken.IsRevoked = true;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, storedToken.User.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, storedToken.User.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var keyBytes = Encoding.UTF8.GetBytes(_jwtOptions.Key);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );
        var newTokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var newRefreshToken = new RefreshToken
        {
            Token = GenerateSecureRefreshToken(),
            Expires = DateTime.UtcNow.AddDays(7),
            UserId = storedToken.User.UserId
        };
        dbContext.RefreshTokens.Remove(storedToken);
        dbContext.RefreshTokens.Add(newRefreshToken);
        dbContext.SaveChanges();

        return Ok(new 
        {
            token = newTokenString,
            refresh_token = newRefreshToken.Token,
            Token_type = "Bearer",
            expires_utc = expires
        });
    }

    private static string GenerateSecureRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}