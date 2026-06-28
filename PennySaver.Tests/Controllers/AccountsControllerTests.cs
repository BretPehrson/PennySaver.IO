namespace PennySaver.Tests.Controllers;

public class AccountsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOnlyLoggedInUserAccounts()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.AddRange(
                new Accounts { Id = 1, UserId = 111, AccountName = "User 1 Account" },
                new Accounts { Id = 2, UserId = 111, AccountName = "User 2 Account" },
                new Accounts { Id = 3, UserId = 999, AccountName = "Malicious Checking" }
            );

            await seedContext.SaveChangesAsync();
        }

        var controller = new AccountsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var accounts = Assert.IsType<List<Accounts>>(okResult.Value);

        Assert.Equal(2, accounts.Count);
        Assert.All(accounts, a => Assert.Equal(111, a.UserId));
    }

    [Fact]
    public async Task Create_ForcesOwnershipToLoggedInUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = new AccountsController(context)
        {
            // Set the controller context to simulate a logged-in user with ID 111
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Accounts
        {
            Id = 5,
            AccountName = "New Account",
            UserId = 999 // Attempt to set to another user
        };

        var result = await controller.Create(incomingPayLoad);

        var createResult = Assert.IsType<CreatedAtActionResult>(result);
        var createdAccount = Assert.IsType<Accounts>(createResult.Value);

        Assert.Equal(111, createdAccount.UserId);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountBelongsToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 10, UserId = 111, CategoryName = "Groceries" });
            seedContext.Accounts.Add(new Accounts { Id = 20, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Transaction
        {
            Id = 1,
            Amount = 100,
            Date = DateTime.Now,
            AccountId = 20, // Belongs to user 999
            CategoryId = 10 // Belongs to user 111, but account ownership should be checked first
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryBelongsToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 10, UserId = 999, CategoryName = "Malicious Category" });
            seedContext.Accounts.Add(new Accounts { Id = 20, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Transaction
        {
            Id = 1,
            Amount = 100,
            AccountId = 20, // Belongs to user 111
            CategoryId = 10 // Belongs to user 999
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Succeeds_WhenAccountAndCategoryBelongToLoggedInUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Transaction
        {
            Id = 1,
            Amount = 100,
            Date = DateTime.Now,
            AccountId = 1, // Belongs to user 111
            CategoryId = 1 // Belongs to user 111
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountDoesNotExist()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Transaction
        {
            Id = 1,
            Amount = 100,
            Date = DateTime.Now,
            AccountId = 999, // Non-existent account
            CategoryId = 1 // Belongs to user 111
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryDoesNotExist()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Transaction
        {
            Id = 1,
            Amount = 100,
            Date = DateTime.Now,
            AccountId = 1, // Belongs to user 111
            CategoryId = 999 // Non-existent category
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountAndCategoryDoNotExist()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Transaction
        {
            Id = 1,
            Amount = 100,
            Date = DateTime.Now,
            AccountId = 999, // Non-existent account
            CategoryId = 999 // Non-existent category
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountAndCategoryBelongToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 10, UserId = 999, CategoryName = "Malicious Category" });
            seedContext.Accounts.Add(new Accounts { Id = 20, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Transaction
        {
            Id = 1,
            Amount = 100,
            Date = DateTime.Now,
            AccountId = 20, // Belongs to user 999
            CategoryId = 10 // Belongs to user 999
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Succeeds_WhenAccountAndCategoryAreValidAndBelongToLoggedInUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };


        var incomingPayLoad = new Transaction
        {
            Id = 1,
            Amount = 100,
            Date = DateTime.Now,
            AccountId = 1, // Belongs to user 111
            CategoryId = 1 // Belongs to user 111
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<OkObjectResult>(result);
    }
}