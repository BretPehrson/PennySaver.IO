namespace PennySaver.Tests.Controllers;

public class AccountControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;
    private readonly IBankSyncService _syncService = new MockBankSyncService();

    public AccountControllerTests()
    {
        _context = TestDbContextFactory.Create();
    }

    private static AccountController CreateController(IDbContextFactory<PennySaverDbContext> context, int? userId = null) => new(context)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOnlyLoggedInUserAccounts()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "malicious_user@example.com", Password = "abcd1234" }
            );

            seedContext.Account.AddRange(
                new Account { Id = 1, UserId = 111, AccountName = "User 1 Account" },
                new Account { Id = 2, UserId = 111, AccountName = "User 2 Account" },
                new Account { Id = 3, UserId = 999, AccountName = "Malicious Checking" }
            );

            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetAll();

        var actionResult = Assert.IsType<ActionResult<IEnumerable<AccountResponseDto>>>(result);
        var accounts = Assert.IsType<List<AccountResponseDto>>(actionResult.Value);

        Assert.Equal(2, accounts.Count);
        Assert.Contains(accounts, a => a.AccountName == "User 1 Account");
        Assert.Contains(accounts, a => a.AccountName == "User 2 Account");
        Assert.DoesNotContain(accounts, a => a.AccountName == "Malicious Checking");
    }

    [Fact]
    public async Task GetAll_Fails_WhenUserIsUnauthorized()
    {
        var controller = CreateController(_context, null); // No user ID

        var result = await controller.GetAll();
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_Fails_WhenUserHasNoAccounts()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetAll();
        var actionResult = Assert.IsType<ActionResult<IEnumerable<AccountResponseDto>>>(result);
        var accounts = Assert.IsType<List<AccountResponseDto>>(actionResult.Value);
        Assert.Empty(accounts);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_Succeeds_WhenAccountExists()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account", Balance = 100.00m, Type = Account.AccountType.Checking });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetById(1);
        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        var account = Assert.IsType<AccountResponseDto>(actionResult.Value);
        Assert.Equal("User 111 Account", account.AccountName);
    }

    [Fact]
    public async Task GetById_Fails_WhenAccountDoesNotExist()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetById(999); // Non-existent account ID
        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetById_Fails_WhenAccountBelongsToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 222, Email = "user222@example.com", Password = "abcd1234" }
            );
            seedContext.Account.Add(new Account { Id = 1, UserId = 222, AccountName = "User 222 Account", Balance = 100.00m, Type = Account.AccountType.Checking });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetById(1);
        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetById_Fails_WhenUserIsNotAuthenticated()
    {
        var controller = CreateController(_context, null); // No user authenticated

        var result = await controller.GetById(1);
        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        Assert.IsType<UnauthorizedResult>(actionResult.Result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_Succeeds_WhenValidDataIsProvided()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            AccountName = "New Account",
            Balance = 100.00m,
            Type = Account.AccountType.Checking
        };

        var result = await controller.Create(incomingPayLoad, _syncService);
        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        var createdResult = Assert.IsType<CreatedAtRouteResult>(actionResult.Result);
        var accountResponse = Assert.IsType<AccountResponseDto>(createdResult.Value);
        Assert.Equal("New Account", accountResponse.AccountName);
        Assert.Equal(100.00m, accountResponse.Balance);
        Assert.Equal(Account.AccountType.Checking, accountResponse.Type);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountNameIsMissing()
    {
        var controller = CreateController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            Balance = 100.00m,
            Type = Account.AccountType.Checking
        };

        var result = await controller.Create(incomingPayLoad, _syncService);
        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        var createResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("The AccountName field is required.", createResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenBalanceIsNegative()
    {
        var controller = CreateController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            AccountName = "New Account",
            Balance = -50.00m,
            Type = Account.AccountType.Checking
        };

        var syncService = new MockBankSyncService();

        var result = await controller.Create(incomingPayLoad, syncService);
        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Balance cannot be negative.", badRequestResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountExistsForUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account", Balance = 100.00m, Type = Account.AccountType.Checking });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            AccountName = "User 111 Account",
            Balance = 100.00m,
            Type = Account.AccountType.Checking
        };

        var result = await controller.Create(incomingPayLoad, _syncService);
        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("An account with the same name already exists for this user.", badRequestResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenInstitutionNameIsTooLong()
    {
        var controller = CreateController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            AccountName = "New Account",
            Institution = new string('A', 101), // 101 characters
            Balance = 100.00m,
            Type = Account.AccountType.Checking
        };

        var result = await controller.Create(incomingPayLoad, _syncService);
        var actoinResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actoinResult.Result);
        Assert.Equal("Institution name cannot exceed 100 characters.", badRequestResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_Succeeds_WhenAccountBelongsToLoggedInUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

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
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "malicious_user@example.com", Password = "abcd1234" }
            );
            seedContext.Account.Add(new Account { Id = 1, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        // log in as 111
        var controller = CreateController(_context, 111);

        var updatePayload = new AccountCreateDto
        {
            AccountName = "Updated Account Name",
            Institution = "Updated Institution",
            Type = Account.AccountType.Savings,
            Balance = 500.00m
        };

        // updating account that belongs to user 999
        var result = await controller.Update(1, updatePayload);

        Assert.IsType<NotFoundResult>(result);
    }


    [Fact]
    public async Task Update_Fails_WhenAccountDoesNotExist()
    {
        var controller = CreateController(_context, 111);

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
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, null); // No user ID

        var updatePayload = new AccountCreateDto
        {
            AccountName = "Updated Account Name",
            Institution = "Updated Institution",
            Type = Account.AccountType.Savings,
            Balance = 500.00m
        };

        var result = await controller.Update(1, updatePayload);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Update_Fails_WhenAccountNameIsMissing()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

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
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

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
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

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

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_Succeeds_WhenAccountBelongsToLoggedInUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenAccountBelongsToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "user999@example.com", Password = "abcd1234" }
            );

            seedContext.Account.Add(new Account { Id = 1, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenAccountDoesNotExist()
    {
        var controller = CreateController(_context, 111);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenUserIsUnauthorized()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, null); // No user ID
        var result = await controller.Delete(1);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenUserIsAuthenticatedButUserIdClaimIsMissing()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, null);
        
        var result = await controller.Delete(1);
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region Plaid Access Token Handling Tests

    [Fact]
    public async Task GetById_DoesNotExposePlaidAccessToken()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account 
            { 
                Id = 1, 
                UserId = 111, 
                AccountName = "User 111 Account", 
                PlaidAccessToken = "sensitive_token_value" 
            });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetById(1);

        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        var accountDto = Assert.IsType<AccountResponseDto>(actionResult.Value);

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
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

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

        var controller = CreateController(_context, 111);

        var result = await controller.GetAll();

        var actionResult = Assert.IsType<ActionResult<IEnumerable<AccountResponseDto>>>(result);
        var accounts = Assert.IsType<List<AccountResponseDto>>(actionResult.Value);

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
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new AccountCreateDto
        {
            AccountName = "Automated Account",
            Institution = "Test Bank",
            Type = Account.AccountType.Checking,
            Balance = 0m,
            IsAutomated = true,
            PlaidAccessToken = "sensitive_token_value"
        };

        var result = await controller.Create(incomingPayLoad, _syncService);

        var actionResult = Assert.IsType<ActionResult<AccountResponseDto>>(result);
        var createdRouteResult = Assert.IsType<CreatedAtRouteResult>(actionResult.Result);
        var createdAccount = Assert.IsType<AccountResponseDto>(createdRouteResult.Value);

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
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;

        public bool IsDevelopment() => EnvironmentName == "Development";
    }

    [Fact]
    public async Task Create_Dev_GeneratesUniqueMockTokens()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }


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

        var controller = CreateController(_context, 111);
        var devEnv = new DevEnv();

        var res1 = await controller.Create(dto1, _syncService, devEnv);
        var actionResult1 = Assert.IsType<ActionResult<AccountResponseDto>>(res1);
        var created1 = Assert.IsType<CreatedAtRouteResult>(actionResult1.Result);
        var acc1 = Assert.IsType<AccountResponseDto>(created1.Value);

        var res2 = await controller.Create(dto2, _syncService, devEnv);
        var actionResult2 = Assert.IsType<ActionResult<AccountResponseDto>>(res2);
        var created2 = Assert.IsType<CreatedAtRouteResult>(actionResult2.Result);
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

    #endregion
}