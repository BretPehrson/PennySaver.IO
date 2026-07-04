namespace PennySaver.Tests.Controllers;

public class AccountControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;

    public AccountControllerTests()
    {
        _context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    }

    private static AccountController CreateAccountController(IDbContextFactory<PennySaverDbContext> sharedContext, int? userId = null) => new(sharedContext)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    private static TransactionController CreateTransactionController(IDbContextFactory<PennySaverDbContext> sharedContext, int? userId = null) => new(sharedContext)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    [Fact]
    public async Task GetAll_ReturnsOnlyLoggedInUserAccounts()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.AddRange(
                new Account { Id = 1, UserId = 111, AccountName = "User 1 Account" },
                new Account { Id = 2, UserId = 111, AccountName = "User 2 Account" },
                new Account { Id = 3, UserId = 999, AccountName = "Malicious Checking" }
            );

            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, 111);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var accounts = Assert.IsType<List<Account>>(okResult.Value);

        Assert.Equal(2, accounts.Count);
        Assert.All(accounts, a => Assert.Equal(111, a.UserId));
    }

    [Fact]
    public async Task Create_Succeeds_WhenAccountNameIsMissing()
    {
        var controller = CreateAccountController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            Balance = 100.00m,
            Type = Account.AccountType.Checking
        };

        var result = await controller.Create(incomingPayLoad);

        var createResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The AccountName field is required.", createResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenBalanceIsNegative()
    {
        var controller = CreateAccountController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            AccountName = "New Account",
            Balance = -50.00m,
            Type = Account.AccountType.Checking
        };

        var result = await controller.Create(incomingPayLoad);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Fails_WhenInstitutionNameIsTooLong()
    {
        var controller = CreateAccountController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            AccountName = "New Account",
            Institution = new string('A', 101), // 101 characters
            Balance = 100.00m,
            Type = Account.AccountType.Checking
        };

        var result = await controller.Create(incomingPayLoad);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountBelongsToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 10, UserId = 111, CategoryName = "Groceries" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(context, 111);

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
            seedContext.Category.Add(new Category { Id = 10, UserId = 999, CategoryName = "Malicious Category" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(context, 111);

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
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(context, 111);

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
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(context, 111);

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
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(context, 111);

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

        var controller = CreateTransactionController(context, 111);

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
            seedContext.Category.Add(new Category { Id = 10, UserId = 999, CategoryName = "Malicious Category" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(context, 111);

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
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(context, 111);

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
    public async Task Update_Succeeds_WhenAccountBelongsToLoggedInUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, 111);

        var updatePayload = new AccountCreateDto
        {
            AccountName = "Updated Account Name",
            Institution = "Updated Institution",
            Type = Account.AccountType.Savings,
            Balance = 500.00m
        };

        var result = await controller.Update(1, updatePayload);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_Fails_WhenAccountBelongsToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, 111);

        var updatePayload = new AccountCreateDto
        {
            AccountName = "Updated Account Name",
            Institution = "Updated Institution",
            Type = Account.AccountType.Savings,
            Balance = 500.00m
        };

        var result = await controller.Update(1, updatePayload);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_Fails_WhenAccountDoesNotExist()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());

        var controller = CreateAccountController(context, 111);

        var updatePayload = new AccountCreateDto
        {
            AccountName = "Updated Account Name",
            Institution = "Updated Institution",
            Type = Account.AccountType.Savings,
            Balance = 500.00m
        };

        var result = await controller.Update(999, updatePayload);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_Fails_WhenUserIsUnauthorized()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, null); // No user ID

        var updatePayload = new AccountCreateDto
        {
            AccountName = "Updated Account Name",
            Institution = "Updated Institution",
            Type = Account.AccountType.Savings,
            Balance = 500.00m
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.Update(1, updatePayload));
    }

    [Fact]
    public async Task Update_Fails_WhenAccountNameIsMissing()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, 111);

        var updatePayload = new AccountCreateDto
        {
            Institution = "Updated Institution",
            Type = Account.AccountType.Savings,
            Balance = 500.00m
        };

        var result = await controller.Update(1, updatePayload);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The AccountName field is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenBalanceIsNegative()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, 111);

        var updatePayload = new AccountCreateDto
        {
            AccountName = "Updated Account Name",
            Institution = "Updated Institution",
            Type = Account.AccountType.Savings,
            Balance = -100.00m
        };

        var result = await controller.Update(1, updatePayload);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Balance cannot be negative.", badRequestResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenInstitutionNameIsTooLong()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, 111);

        var updatePayload = new AccountCreateDto
        {
            AccountName = "Updated Account Name",
            Institution = new string('A', 101), // 101 characters
            Type = Account.AccountType.Savings,
            Balance = 500.00m
        };

        var result = await controller.Update(1, updatePayload);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Institution name cannot exceed 100 characters.", badRequestResult.Value);
    }

    [Fact]
    public async Task Delete_Succeeds_WhenAccountBelongsToLoggedInUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, 111);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenAccountBelongsToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, 111);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenAccountDoesNotExist()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());

        var controller = CreateAccountController(context, 111);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenUserIsUnauthorized()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, null); // No user ID

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.Delete(1));
    }

    [Fact]
    public async Task Delete_Fails_WhenUserIsAuthenticatedButUserIdClaimIsMissing()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(context, null);
        
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.Delete(1));
    }
}