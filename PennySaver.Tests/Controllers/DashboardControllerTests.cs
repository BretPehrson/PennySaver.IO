namespace PennySaver.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;

    public DashboardControllerTests()
    {
        _context = TestDbContextFactory.Create();
    }
    
    private static DashboardController CreateDashboardController(IDbContextFactory<PennySaverDbContext> context, int? userId = null) => new(context)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    [Fact]
    public async Task GetOverview_ReturnsCorrectTotals_ForValidUserData()
    {
        using (var context = await _context.CreateDbContextAsync())
        {
            context.User.AddRange(
                new User { UserId = 1, Email = "user1@example.com", Password = "password" },
                new User { UserId = 2, Email = "user2@example.com", Password = "password" }
            );
            await context.SaveChangesAsync();

            var acct1 = new Account { Id = 1, UserId = 1, Balance = 1000m };
            var acct2 = new Account { Id = 2, UserId = 1, Balance = 500m };
            var acct3 = new Account { Id = 3, UserId = 2, Balance = 2000m }; // Should not be included in totals
            context.Account.AddRange(acct1, acct2, acct3);
            await context.SaveChangesAsync();

            context.Category.AddRange(
                new Category { Id = 1, UserId = 1, Name = "Category 1" },
                new Category { Id = 2, UserId = 1, Name = "Category 2" }
            );
            await context.SaveChangesAsync();

            context.Budget.Add(new Budget
            {
                UserId = 1,
                CategoryId = 1,
                Month = DateTime.UtcNow.Month,
                Year = DateTime.UtcNow.Year,
                TargetAmount = 1500m
            });
            await context.SaveChangesAsync();

            var transaction1 = new Transaction
            {
                AccountId = acct1.Id,
                CategoryId = 1,
                Amount = 200m,
                Date = DateTime.UtcNow.AddDays(-5) // This month
            };
            var transaction2 = new Transaction
            {                
                AccountId = acct2.Id,
                CategoryId = 2,
                Amount = 300m,
                Date = DateTime.UtcNow.AddDays(-10) // This month
            };
            context.Transaction.AddRange(transaction1, transaction2);
            await context.SaveChangesAsync();
        }

        // Arrange
        var userId = 1;
        var controller = CreateDashboardController(_context, userId);

        // Act
        var result = await controller.GetOverview();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var overview = Assert.IsType<DashboardDto>(okResult.Value);
                
        Assert.Equal(1500m, overview.TotalCash);
        Assert.Equal(1500m, overview.MonthlyBudget);
        Assert.Equal(1000m, overview.RemainingBudget);
    }

    [Fact]
    public async Task GetOverview_CompleteExcludesOtherUsersData()
    {
        using (var context = await _context.CreateDbContextAsync())
        {
            context.User.AddRange(
                new User { UserId = 1, Email = "user1@example.com", Password = "password" },
                new User { UserId = 2, Email = "user2@example.com", Password = "password" }
            );
            await context.SaveChangesAsync();

            var acct1 = new Account { Id = 1, UserId = 1, Balance = 1000m };
            var acct2 = new Account { Id = 2, UserId = 1, Balance = 500m };
            var acct3 = new Account { Id = 3, UserId = 2, Balance = 2000m }; // Should not be included in totals
            context.Account.AddRange(acct1, acct2, acct3);
            await context.SaveChangesAsync();

            context.Category.AddRange(
                new Category { Id = 1, UserId = 1, Name = "Category 1" },
                new Category { Id = 2, UserId = 1, Name = "Category 2" },
                new Category { Id = 3, UserId = 2, Name = "Category 3" }
            );
            await context.SaveChangesAsync();

            var budget1 = new Budget
            {
                UserId = 1,
                CategoryId = 1,
                Month = DateTime.UtcNow.Month,
                Year = DateTime.UtcNow.Year,
                TargetAmount = 1500m
            };
            var budget2 = new Budget
            {
                UserId = 2,
                CategoryId = 3,
                Month = DateTime.UtcNow.Month,
                Year = DateTime.UtcNow.Year,
                TargetAmount = 3000m // Should not be included in totals
            };
            context.Budget.AddRange(budget1, budget2);
            await context.SaveChangesAsync();

            var transaction1 = new Transaction
            {
                AccountId = acct1.Id,
                CategoryId = 1,
                Amount = 200m,
                Date = DateTime.Now.Date.AddDays(-1).Month == DateTime.UtcNow.Month ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow
            };
            var transaction2 = new Transaction
            {
                AccountId = acct2.Id,
                CategoryId = 2,
                Amount = 300m,
                Date = DateTime.Now.Date.AddDays(-3).Month == DateTime.UtcNow.Month ? DateTime.UtcNow.AddDays(-3) : DateTime.UtcNow
            };
            var transaction3 = new Transaction
            {
                AccountId = acct3.Id,
                CategoryId = 3,
                Amount = 500m,
                Date = DateTime.Now.Date.AddDays(-8).Month == DateTime.UtcNow.Month ? DateTime.UtcNow.AddDays(-8) : DateTime.UtcNow
            };
            context.Transaction.AddRange(transaction1, transaction2, transaction3);
            await context.SaveChangesAsync();
        }

        var userId = 1;
        var controller = CreateDashboardController(_context, userId);

        var result = await controller.GetOverview();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var overview = Assert.IsType<DashboardDto>(okResult.Value);

        Assert.Equal(1500m, overview.TotalCash);
        Assert.Equal(1500m, overview.MonthlyBudget);
        Assert.Equal(1000m, overview.RemainingBudget);
    }

    [Fact]
    public async Task GetOverview_NoBudgets_ReturnsZeroRemainingBudget()
    {
        using (var context = await _context.CreateDbContextAsync())
        {
            context.User.Add(new User { UserId = 1, Email = "user1@example.com", Password = "password" });
            await context.SaveChangesAsync();

            var acct1 = new Account { Id = 1, UserId = 1, Balance = 1000m };
            context.Account.Add(acct1);
            await context.SaveChangesAsync();
        }

        var userId = 1;
        var controller = CreateDashboardController(_context, userId);

        var result = await controller.GetOverview();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var overview = Assert.IsType<DashboardDto>(okResult.Value);

        Assert.Equal(1000m, overview.TotalCash);
        Assert.Equal(0m, overview.MonthlyBudget);
        Assert.Equal(0m, overview.RemainingBudget);
    }

    [Fact]
    public async Task GetOverview_NoAccounts_ReturnsZeroTotalCash()
    {
        using (var context = await _context.CreateDbContextAsync())
        {
            context.User.Add(new User { UserId = 1, Email = "user1@example.com", Password = "password" });
            await context.SaveChangesAsync();
        }

        var userId = 1;
        var controller = CreateDashboardController(_context, userId);

        var result = await controller.GetOverview();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var overview = Assert.IsType<DashboardDto>(okResult.Value);

        Assert.Equal(0m, overview.TotalCash);
        Assert.Equal(0m, overview.MonthlyBudget);
        Assert.Equal(0m, overview.RemainingBudget);
    }

    [Fact]
    public async Task GetOverview_NoUsers_ReturnsZeroOverview()
    {
        var controller = CreateDashboardController(_context, 1);

        var result = await controller.GetOverview();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var overview = Assert.IsType<DashboardDto>(okResult.Value);

        Assert.Equal(0m, overview.TotalCash);
        Assert.Equal(0m, overview.MonthlyBudget);
        Assert.Equal(0m, overview.RemainingBudget);
    }

    [Fact]
    public async Task GetOverview_NoTransactions_ReturnsCorrectRemainingBudget()
    {
        using (var context = await _context.CreateDbContextAsync())
        {
            context.User.Add(new User { UserId = 1, Email = "user1@example.com", Password = "password" });
            await context.SaveChangesAsync();

            var acct1 = new Account { Id = 1, UserId = 1, Balance = 1000m };
            context.Account.Add(acct1);
            await context.SaveChangesAsync();

            context.Category.Add(new Category { Id = 1, UserId = 1, Name = "Category 1" });
            await context.SaveChangesAsync();

            context.Budget.Add(new Budget
            {
                UserId = 1,
                CategoryId = 1,
                Month = DateTime.UtcNow.Month,
                Year = DateTime.UtcNow.Year,
                TargetAmount = 1500m
            });
            await context.SaveChangesAsync();
        }

        var userId = 1;
        var controller = CreateDashboardController(_context, userId);

        var result = await controller.GetOverview();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var overview = Assert.IsType<DashboardDto>(okResult.Value);

        Assert.Equal(1000m, overview.TotalCash);
        Assert.Equal(1500m, overview.MonthlyBudget);
        Assert.Equal(1500m, overview.RemainingBudget);
    }
}