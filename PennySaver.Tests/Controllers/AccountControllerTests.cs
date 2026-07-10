namespace PennySaver.Tests.Controllers;

public class AccountControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;
    private readonly IBankSyncService _syncService = new MockBankSyncService();

    public AccountControllerTests()
    {
        _context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    }

    private static AccountController CreateAccountController(IDbContextFactory<PennySaverDbContext> context, int? userId = null) => new(context)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    private static TransactionController CreateTransactionController(IDbContextFactory<PennySaverDbContext> context, int? userId = null) => new(context)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };


    [Fact]
    public async Task GetAll_ReturnsOnlyLoggedInUserAccounts()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.AddRange(
                new Account { Id = 1, UserId = 111, AccountName = "User 1 Account" },
                new Account { Id = 2, UserId = 111, AccountName = "User 2 Account" },
                new Account { Id = 3, UserId = 999, AccountName = "Malicious Checking" }
            );

            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var accounts = Assert.IsType<List<AccountResponseDto>>(okResult.Value);

        Assert.Equal(2, accounts.Count);
        Assert.Contains(accounts, a => a.AccountName == "User 1 Account");
        Assert.Contains(accounts, a => a.AccountName == "User 2 Account");
        Assert.DoesNotContain(accounts, a => a.AccountName == "Malicious Checking");
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

        var result = await controller.Create(incomingPayLoad, _syncService);

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

        var syncService = new MockBankSyncService();

        var result = await controller.Create(incomingPayLoad, syncService);
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

        var result = await controller.Create(incomingPayLoad, _syncService);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountBelongsToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 10, UserId = 111, CategoryName = "Groceries" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 10, UserId = 999, CategoryName = "Malicious Category" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(_context, 111);

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
        var controller = CreateTransactionController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 10, UserId = 999, CategoryName = "Malicious Category" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateTransactionController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

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
        var controller = CreateAccountController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, null); // No user ID

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenAccountBelongsToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenAccountDoesNotExist()
    {
        var controller = CreateAccountController(_context, 111);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenUserIsUnauthorized()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, null); // No user ID

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.Delete(1));
    }

    [Fact]
    public async Task Delete_Fails_WhenUserIsAuthenticatedButUserIdClaimIsMissing()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, null);
        
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.Delete(1));
    }

    [Fact]
    public async Task GetById_DoesNotExposePlaidAccessToken()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account 
            { 
                Id = 1, 
                UserId = 111, 
                AccountName = "User 111 Account", 
                PlaidAccessToken = "sensitive_token_value" 
            });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

        var result = await controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var accountDto = Assert.IsType<AccountResponseDto>(okResult.Value);

        var json = System.Text.Json.JsonSerializer.Serialize(accountDto);
        Assert.DoesNotContain("sensitive_token_value", json, StringComparison.OrdinalIgnoreCase);

        Assert.Equal("User 111 Account", accountDto.AccountName);
        Assert.Null(typeof(AccountResponseDto).GetProperty("PlaidAccessToken"));
    }

    [Fact]
    public async Task GetAll_DoesNotExposePlaidAccessTokens()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.AddRange(
                new Account 
                { 
                    Id = 1, 
                    UserId = 111, 
                    AccountName = "User 111 Account", 
                    PlaidAccessToken = "sensitive_token_value_1" 
                },
                new Account 
                { 
                    Id = 2, 
                    UserId = 111, 
                    AccountName = "User 111 Savings", 
                    PlaidAccessToken = "sensitive_token_value_2" 
                }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateAccountController(_context, 111);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var accounts = Assert.IsType<List<AccountResponseDto>>(okResult.Value);

        foreach (var account in accounts)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(account);
            Assert.DoesNotContain("sensitive_token_value_1", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sensitive_token_value_2", json, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Null(typeof(AccountResponseDto).GetProperty("PlaidAccessToken"));
    }

    [Fact]
    public async Task Create_Automated_ReturnsSafeDto_AndStoresTokenInternally()
    {
        var controller = CreateAccountController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            AccountName = "Automated Account",
            Institution = "Test Bank",
            Type = Account.AccountType.Checking,
            Balance = 100.00m,
            IsAutomated = true,
            PlaidAccessToken = "sensitive_token_value"
        };

        var result = await controller.Create(incomingPayLoad, _syncService);

        var okResult = Assert.IsType<CreatedAtActionResult>(result);
        var createdAccount = Assert.IsType<AccountResponseDto>(okResult.Value);

        Assert.Equal("Automated Account", createdAccount.AccountName);
        Assert.Equal("Test Bank", createdAccount.Institution);
        Assert.Equal(Account.AccountType.Checking, createdAccount.Type);
        Assert.True(createdAccount.IsAutomated);
        Assert.Null(typeof(AccountResponseDto).GetProperty("PlaidAccessToken"));

        // Verify that the token is stored internally in the database
        using var verifyContext = await _context.CreateDbContextAsync();
        var accountInDb = await verifyContext.Account.FirstOrDefaultAsync(a => a.AccountName == "Automated Account" && a.UserId == 111);
        Assert.NotNull(accountInDb);
        Assert.Equal("sensitive_token_value", accountInDb.PlaidAccessToken); // Token should be stored internally
    }

    private class DevEnv : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = string.Empty;
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider? WebRootFileProvider { get; set; }
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider? ContentRootFileProvider { get; set; }

        public bool IsDevelopment() => EnvironmentName == "Development";
    }

    [Fact]
    public async Task Create_Dev_GeneratesUniqueMockTokens()
    {
        var controller = CreateAccountController(_context, 111);
        var devEnv = new DevEnv();

        var dto1 = new AccountCreateDto
        {
            AccountName = "Dev Account 1",
            Institution = "Test Bank",
            Type = Account.AccountType.Checking,
            Balance = 0m,
            IsAutomated = true,
            PlaidAccessToken = null
        };

        var dto2 = new AccountCreateDto
        {
            AccountName = "Dev Account 2",
            Institution = "Test Bank",
            Type = Account.AccountType.Checking,
            Balance = 0m,
            IsAutomated = true,
            PlaidAccessToken = null
        };

        var res1 = await controller.Create(dto1, _syncService, devEnv);
        var created1 = Assert.IsType<CreatedAtActionResult>(res1);
        var acc1 = Assert.IsType<AccountResponseDto>(created1.Value);

        var res2 = await controller.Create(dto2, _syncService, devEnv);
        var created2 = Assert.IsType<CreatedAtActionResult>(res2);
        var acc2 = Assert.IsType<AccountResponseDto>(created2.Value);

        using var verifyContext = await _context.CreateDbContextAsync();
        var a1 = await verifyContext.Account.FirstOrDefaultAsync(a => a.AccountName == "Dev Account 1" && a.UserId == 111);
        var a2 = await verifyContext.Account.FirstOrDefaultAsync(a => a.AccountName == "Dev Account 2" && a.UserId == 111);

        Assert.NotNull(a1);
        Assert.NotNull(a2);
        Assert.NotNull(a1.PlaidAccessToken);
        Assert.NotNull(a2.PlaidAccessToken);
        Assert.NotEqual(a1.PlaidAccessToken, a2.PlaidAccessToken);
    }
}