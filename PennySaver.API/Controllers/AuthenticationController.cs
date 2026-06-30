namespace PennySaver.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IDbContextFactory<PennySaverDbContext> dbContextFactory, IOptions<JwtOption> jwtOptions) : ControllerBase
{
    private readonly IDbContextFactory<PennySaverDbContext> _dbContextFactory = dbContextFactory;
    private readonly JwtOption _jwtOptions = jwtOptions.Value;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User model)
    {
        if ( model.Email == null || model.PasswordHash == null)
            return BadRequest(new { message = "Email and password are required." });

        using var context = _dbContextFactory.CreateDbContext();

        if (context.User.Any(u => u.Email == model.Email))
            return BadRequest(new { message = "Email is already registered." });

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
        var user = new User
        {
            Email = model.Email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        context.User.Add(user);
        await context.SaveChangesAsync();

        return Ok(new { message = "Registration successful." });
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<IActionResult> IssueToken([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        using var context = _dbContextFactory.CreateDbContext();
        
        var user = await context.User
                .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return Unauthorized(new { message = "Invalid username or password." });

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid) return Unauthorized(new { message = "Invalid username or password." });

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

        context.RefreshToken.Add(newRefreshToken);
        await context.SaveChangesAsync();

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
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Refresh token is missing." });

        using var context = _dbContextFactory.CreateDbContext();
        
        RefreshToken? storedToken = context.RefreshToken
            .Include(t => t.User)
            .FirstOrDefault(rt => rt.Token == refreshToken);

        if (storedToken == null || !storedToken.IsActive)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

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
        context.RefreshToken.Remove(storedToken);
        context.RefreshToken.Add(newRefreshToken);
        await context.SaveChangesAsync();

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