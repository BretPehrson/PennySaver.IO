namespace PennySaver.Tests.Controllers;

public class AuthenticationControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;
    private readonly IOptions<JwtOption> _jwtOptions;

    public AuthenticationControllerTests()
    {
        _context = TestDbContextFactory.Create();

        var jwtOptions = new JwtOption
        {
            Key = "This_Is_A_Secret_Key_For_Jwt_Token_Generation_123!",
            Issuer = "PennySaver",
            Audience = "PennySaverUsers",
            ExpiryMinutes = 5
        };
        _jwtOptions = Options.Create(jwtOptions);
    }

    private AuthController CreateController() => new(_context, _jwtOptions)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(null)
    };

    [Fact]
    public async Task Register_WithNewEmail_ReturnsOkResult()
    {
        var controller = CreateController();

        var newUser = new User
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var result = await controller.Register(newUser);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var response = new RouteValueDictionary(okResult.Value);
        Assert.Equal("Registration successful.", response["message"]);

        var savedUser = _context.CreateDbContext().User.FirstOrDefault(u => u.Email == newUser.Email);
        Assert.NotNull(savedUser);
        Assert.Equal(newUser.Email, savedUser.Email);

        Assert.NotEqual(newUser.Password, savedUser.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", savedUser.Password));
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_ForExistingEmail()
    {
        var controller = CreateController();

        var newUser = new User
        {
            Email = "test@example.com",
            Password = "Password123!",
            CreatedAt = DateTime.UtcNow
        };
        var newUserResult = await controller.Register(newUser);
        var okResult = Assert.IsType<OkObjectResult>(newUserResult);
        Assert.NotNull(okResult.Value);

        var duplicateUser = new User
        {
            Email = "test@example.com",
            Password = "DifferentPassword123!"
        };
        var result = await controller.Register(duplicateUser);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var data = new RouteValueDictionary(badRequestResult.Value);
        Assert.Equal("Email is already registered.", data["message"]);
    }

    [Fact]
    public async Task IssueToken_ReturnsUnauthorized_ForInvalidCredentials()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = "test@example.com",
            Password = "Password123!"
        };
        var result = await controller.Register(user);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var invalidUser = new LoginRequest(user.Email, "WrongPassword!");

        var tokenResult = await controller.IssueToken(invalidUser);
        Assert.IsType<UnauthorizedObjectResult>(tokenResult);
    }

    [Fact]
    public async Task IssueToken_ReturnsOk_ForValidCredentials()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = "test@example.com",
            Password = "Password123!"
        };
        var result = await controller.Register(user);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var validUser = new LoginRequest(user.Email, "Password123!");

        var tokenResult = await controller.IssueToken(validUser);
        Assert.IsType<OkObjectResult>(tokenResult);
        var tokenData = new RouteValueDictionary(((OkObjectResult)tokenResult).Value);
        Assert.NotNull(tokenData["token"]);
    }

    [Fact]
    public async Task RefreshToken_ReturnsUnauthorized_WhenNoTokenProvided()
    {
        var controller = CreateController();
        var result = await controller.Refresh();
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RefreshToken_ReturnsUnauthorized_ForInvalidToken()
    {
        var controller = CreateController();

        controller.ControllerContext.HttpContext.Request.Headers.Append("Cookie", "refreshToken=InvalidToken");
        var result = await controller.Refresh();
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RefreshToken_ReturnsOk_ForValidToken()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var targetTokenString = "A_Valid_Mock_Base64_Encoded_Token_String_For_Testing_Purposes";
        var validRefreshToken = new RefreshToken
        {
            Token = targetTokenString,
            Expires = DateTime.UtcNow.AddMinutes(10),
            UserId = user.UserId,
            User = user
        };

        using var arrangeContext = _context.CreateDbContext();
        arrangeContext.User.Add(user);
        arrangeContext.RefreshToken.Add(validRefreshToken);
        await arrangeContext.SaveChangesAsync();

        controller.ControllerContext.HttpContext.Request.Headers.Append("Cookie", $"refreshToken={targetTokenString}");
        var result = await controller.Refresh();
        
        var refreshOkResult = Assert.IsType<OkObjectResult>(result);
        var tokenData = new RouteValueDictionary(refreshOkResult.Value);

        Assert.NotNull(tokenData["token"]);
        Assert.NotNull(tokenData["expires_utc"]);
        Assert.Equal("Bearer", tokenData["Token_type"]);
    }
    
    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEmailIsNull()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = null!,
            Password = "Password123!"
        };

        var result = await controller.Register(user);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var data = new RouteValueDictionary(badRequestResult.Value);
        Assert.Equal("Email and password are required.", data["message"]);
    }
    
    [Fact]
    public async Task Register_ReturnsBadRequest_WhenPasswordIsNull()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = "test@example.com",
            Password = null!
        };

        var result = await controller.Register(user);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var data = new RouteValueDictionary(badRequestResult.Value);
        Assert.Equal("Email and password are required.", data["message"]);
    }

    [Fact]
    public async Task IssueToken_ReturnsUnauthorized_ForNonExistentEmail()
    {
        var controller = CreateController();

        var loginRequest = new LoginRequest("nonexistent@example.com", "Password123!");
        var result = await controller.IssueToken(loginRequest);
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var data = new RouteValueDictionary(unauthorizedResult.Value);
        Assert.Equal("Invalid username or password.", data["message"]);
    }

    [Fact]
    public async Task RefreshToken_ReturnsUnauthorized_WhenTokenIsExpired()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var expiredRefreshToken = new RefreshToken
        {
            Token = "ExpiredTokenString",
            Expires = DateTime.UtcNow.AddMinutes(-10),
            UserId = user.UserId,
            User = user
        };

        using var arrangeContext = _context.CreateDbContext();
        arrangeContext.User.Add(user);
        arrangeContext.RefreshToken.Add(expiredRefreshToken);
        await arrangeContext.SaveChangesAsync();

        controller.ControllerContext.HttpContext.Request.Headers.Append("Cookie", $"refreshToken={expiredRefreshToken.Token}");
        var result = await controller.Refresh();
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var data = new RouteValueDictionary(unauthorizedResult.Value);
        Assert.Equal("Invalid or expired refresh token.", data["message"]);
    }

    [Fact]
    public async Task RefreshToken_RotatesTokens_AndDeletesOldToken()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var validRefreshToken = new RefreshToken
        {
            Token = "ValidTokenString",
            Expires = DateTime.UtcNow.AddMinutes(10),
            UserId = user.UserId,
            User = user
        };

        using (var arrangeContext = _context.CreateDbContext())
        {
            arrangeContext.User.Add(user);
            arrangeContext.RefreshToken.Add(validRefreshToken);
            await arrangeContext.SaveChangesAsync();
        }

        controller.ControllerContext.HttpContext.Request.Headers.Append("Cookie", $"refreshToken={validRefreshToken.Token}");
        var result = await controller.Refresh();

        var refreshOkResult = Assert.IsType<OkObjectResult>(result);
        var tokenData = new RouteValueDictionary(refreshOkResult.Value);
        Assert.NotNull(tokenData["token"]);

        var setCookieHeader = controller.ControllerContext.HttpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("refreshToken=", setCookieHeader);
        Assert.DoesNotContain("refreshToken=ValidTokenString", setCookieHeader);

        using (var verifyContext = _context.CreateDbContext())
        {
            var oldToken = verifyContext.RefreshToken.FirstOrDefault(rt => rt.Token == validRefreshToken.Token);
            Assert.Null(oldToken);

            var newToken = verifyContext.RefreshToken.Where(t => t.UserId == user.UserId);
            Assert.Single(newToken);
            Assert.NotEqual(validRefreshToken.Token, newToken.First().Token);
        }
    }
}