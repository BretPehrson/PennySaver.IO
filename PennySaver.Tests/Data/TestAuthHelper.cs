namespace PennySaver.Tests.Data;

public static class TestAuthHelper
{
    public static ControllerContext GetControllerContext(int? userId)
    {
        var user = new List<Claim> 
        {
             new(JwtRegisteredClaimNames.Sub, userId?.ToString() ?? string.Empty),
             new(ClaimTypes.NameIdentifier, userId?.ToString() ?? string.Empty)
        };
        
        var identity = new ClaimsIdentity(user, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}