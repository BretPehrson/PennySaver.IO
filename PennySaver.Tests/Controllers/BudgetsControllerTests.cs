namespace PennySaver.Tests.Controllers;

public class FinanceControllerTests
{
        public static ControllerContext GetControllerContext(int userId)
    {
        var user = new List<Claim> 
        {
             new(JwtRegisteredClaimNames.Sub, userId.ToString()),
             new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        
        var identity = new ClaimsIdentity(user, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}