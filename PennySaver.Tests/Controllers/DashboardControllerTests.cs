namespace PennySaver.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    
    private static DashboardController CreateDashboardController(IDbContextFactory<PennySaverDbContext> context, int? userId = null) => new(context)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    [Fact]
    public async Task GetOverview_ReturnsOkResult_WithDashboardDto()
    {
    }
}