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

        SetRefreshTokenCookie(newRefreshToken.Token);
        return Ok(new 
        {
             token = tokenString,
             Token_type = "Bearer",
             expires_utc = expires
        });
    }


    [AllowAnonymous]
    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Refresh token is missing." });

        using var dbContext = _dbContextFactory.CreateDbContext();
        
        RefreshToken? storedToken = dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefault(rt => rt.Token == refreshToken);

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

        SetRefreshTokenCookie(newRefreshToken.Token);
        return Ok(new 
        {
            token = newTokenString,
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

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}