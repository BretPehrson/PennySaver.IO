namespace PennySaver.Tests.Controllers;

public class BudgetsControllerTests
{
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
    public async Task Verify_UpdateCategory()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Categories.Add(new Category { Id = 2, UserId = 111, CategoryName = "Transport" });
            seedContext.Budgets.Add(new Budgets { Id = 1, UserId = 111, TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = new BudgetsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var updatePayload = new Budgets
        {
            Id = 1,
            TargetAmount = 75,
            CategoryId = 2, // Change to a different category owned by the same user
        };

        var result = await controller.Update(1, updatePayload);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_CategoryMustBelongToUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Categories.Add(new Category { Id = 2, UserId = 999, CategoryName = "Transport" });
            seedContext.Budgets.Add(new Budgets { Id = 1, UserId = 111, TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = new BudgetsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var updatePayload = new Budgets
        {
            Id = 1,
            TargetAmount = 75,
            CategoryId = 2, // Attempt to change to a category owned by another user
        };

        var result = await controller.Update(1, updatePayload);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal("Invalid category assignment.", badRequestResult.Value);
    }

    [Fact]
    public async Task Update_NotFoundForOwner()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Budgets.Add(new Budgets { Id = 1, UserId = 111, TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = new BudgetsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var updatePayload = new Budgets
        {
            Id = 999, // Non-existent budget ID
            TargetAmount = 75,
            CategoryId = 1,
        };

        var result = await controller.Update(999, updatePayload);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_BadRequestForIdMismatch()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Budgets.Add(new Budgets { Id = 1, UserId = 111, TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = new BudgetsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var updatePayload = new Budgets
        {
            Id = 2, // Mismatched ID
            TargetAmount = 75,
            CategoryId = 1,
        };

        var result = await controller.Update(1, updatePayload);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_RemovesBudget()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Budgets.Add(new Budgets { Id = 1, UserId = 111, TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = new BudgetsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.Delete(1);
        Assert.IsType<NoContentResult>(result);

        // Verify it's actually deleted
        using var verifyContext = context.CreateDbContext();
        var budget = await verifyContext.Budgets.FindAsync(1);
        Assert.Null(budget);
    }

    [Fact]
    public async Task Delete_NotFoundForNonExistentBudget()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = new BudgetsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.Delete(999); // Non-existent budget ID
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_NotFoundForOtherUsersBudget()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 999, CategoryName = "Food" });
            seedContext.Budgets.Add(new Budgets { Id = 1, UserId = 999, TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = new BudgetsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111) // Logged in as a different user
        };

        var result = await controller.Delete(1); // Attempt to delete another user's budget
        Assert.IsType<NotFoundResult>(result);
    }
}