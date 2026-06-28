namespace PennySaver.Tests.Controllers;

public class BudgetsControllerTests
{
    [Fact]
    public async Task Verify_GetAll()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.AddRange(
                new Category { Id = 1, UserId = 111, CategoryName = "Food" },
                new Category { Id = 2, UserId = 111, CategoryName = "Transport" },
                new Category { Id = 3, UserId = 999, CategoryName = "Entertainment" }
            );
            seedContext.Budgets.AddRange(
                new Budgets { Id = 1, UserId = 111, TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030 },
                new Budgets { Id = 2, UserId = 111, TargetAmount = 100, CategoryId = 2, Month = 1, Year = 2030 },
                new Budgets { Id = 3, UserId = 999, TargetAmount = 200, CategoryId = 3, Month = 1, Year = 2030 }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = new BudgetsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.GetAll();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var budgets = Assert.IsType<List<Budgets>>(okResult.Value);

        Assert.Equal(2, budgets.Count);
        Assert.All(budgets, b => Assert.Equal(111, b.UserId));

        //Get user 999's budgets
        controller.ControllerContext = TestAuthHelper.GetControllerContext(999);
        result = await controller.GetAll();
        okResult = Assert.IsType<OkObjectResult>(result);
        budgets = Assert.IsType<List<Budgets>>(okResult.Value);
        Assert.Single(budgets);
        Assert.All(budgets, b => Assert.Equal(999, b.UserId));
    }

    [Fact]
    public async Task Create_ForcesOwnershipToLoggedInUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());

        // Seed a category that belongs to our logged-in user (111)
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new BudgetsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Budgets
        {
            Id = 5,
            TargetAmount = 150,
            CategoryId = 1, // This category belongs to user 111
            UserId = 999 // Attempt to set to another user
        };

        var result = await controller.Create(incomingPayLoad);

        var createResult = Assert.IsType<OkObjectResult>(result);
        var createdBudget = Assert.IsType<Budgets>(createResult.Value);

        Assert.Equal(111, createdBudget.UserId);
    }
}