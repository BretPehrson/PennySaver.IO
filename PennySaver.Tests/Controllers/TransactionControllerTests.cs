namespace PennySaver.Tests.Controllers;

public class TransactionControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;

    public TransactionControllerTests()
    {
        _context = TestDbContextFactory.Create();
    }

    private static TransactionController CreateController(IDbContextFactory<PennySaverDbContext> sharedContext, int? userId = null) => new(sharedContext)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithTransactions()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.AddRange(
                new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed },
                new Transaction { Id = 2, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Completed }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetAll(null);

        var okResult = Assert.IsType<ActionResult<IEnumerable<TransactionResponseDto>>>(result);
        var transactions = Assert.IsType<List<TransactionResponseDto>>(okResult.Value);
        Assert.Equal(2, transactions.Count);
        Assert.Contains(transactions, t => t.Id == 1 && t.Description == "Groceries");
        Assert.Contains(transactions, t => t.Id == 2 && t.Description == "Dining Out");
    }

    [Fact]
    public async Task GetAll_FiltersByStatusCompleted()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.AddRange(
                new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed },
                new Transaction { Id = 2, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Pending },
                new Transaction { Id = 3, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Voided }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetAll(TransactionStatus.Completed);
        var okResult = Assert.IsType<ActionResult<IEnumerable<TransactionResponseDto>>>(result);
        var transactions = Assert.IsType<List<TransactionResponseDto>>(okResult.Value);
        Assert.Single(transactions);
        Assert.Equal(TransactionStatus.Completed, transactions[0].Status);
        Assert.Equal(1, transactions[0].Id);
    }

    [Fact]
    public async Task GetAll_FiltersByStatusPending()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.AddRange(
                new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed },
                new Transaction { Id = 2, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Pending },
                new Transaction { Id = 3, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Voided }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetAll(TransactionStatus.Pending);
        var okResult = Assert.IsType<ActionResult<IEnumerable<TransactionResponseDto>>>(result);
        var transactions = Assert.IsType<List<TransactionResponseDto>>(okResult.Value);
        Assert.Single(transactions);
        Assert.Equal(TransactionStatus.Pending, transactions[0].Status);
        Assert.Equal(2, transactions[0].Id);
    }

    [Fact]
    public async Task GetAll_FiltersByStatusNotVoided()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.AddRange(
                new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed },
                new Transaction { Id = 2, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Pending },
                new Transaction { Id = 3, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Voided }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetAll(null);
        var okResult = Assert.IsType<ActionResult<IEnumerable<TransactionResponseDto>>>(result);
        var transactions = Assert.IsType<List<TransactionResponseDto>>(okResult.Value);

        Assert.Equal(2, transactions.Count);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ReturnsOkResult_WithTransaction()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetById(1);
        var okResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var transaction = Assert.IsType<OkObjectResult>(okResult.Result);
        var createdTransaction = Assert.IsType<TransactionResponseDto>(transaction.Value);

        Assert.Equal(1, createdTransaction.Id);
        Assert.Equal(1, createdTransaction.AccountId);
        Assert.Equal(1, createdTransaction.CategoryId);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForNonExistentTransaction()
    {
        var controller = CreateController(_context, 111);

        var result = await controller.GetById(999);
        var notFoundResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var notFound = Assert.IsType<NotFoundResult>(notFoundResult.Result);

        Assert.IsType<NotFoundResult>(notFound);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForTransactionBelongingToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 999); // Different user

        var result = await controller.GetById(1);
        var notFoundResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var notFound = Assert.IsType<NotFoundResult>(notFoundResult.Result);

        Assert.IsType<NotFoundResult>(notFound);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_Succeeds_ForValidTransaction()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new TransactionCreateDto
        {
            Amount = 100,
            Description = "Valid Transaction",
            Status = TransactionStatus.Completed,
            AccountId = 1,
            CategoryId = 1
        };

        var result = await controller.Create(incomingPayLoad);

        var okResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var createdResult = Assert.IsType<CreatedAtRouteResult>(okResult.Result);
        var createdTransaction = Assert.IsType<TransactionResponseDto>(createdResult.Value);

        Assert.Equal(100, createdTransaction.Amount);
        Assert.Equal("Valid Transaction", createdTransaction.Description);
        Assert.Equal(TransactionStatus.Completed, createdTransaction.Status);
        Assert.Equal(1, createdTransaction.AccountId);
        Assert.Equal(1, createdTransaction.CategoryId);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountOrCategoryBelongsToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "user999@example.com", Password = "abcd1234" }
                );

            seedContext.Category.Add(new Category { Id = 10, UserId = 111, Name = "Groceries" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);


        var incomingPayLoad = new TransactionCreateDto
        {
            Amount = 100,
            Description = "Hacked Transaction",
            Status = TransactionStatus.Completed,
            AccountId = 20, // Belongs to user 999
            CategoryId = 10 // Belongs to user 111
        };

        var result = await controller.Create(incomingPayLoad);

        var badRequestResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Equal("Invalid account or category assignment.", badRequest.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountBelongsToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "malicious_user@example.com", Password = "abcd1234" }
            );

            seedContext.Category.Add(new Category { Id = 10, UserId = 111, Name = "Groceries" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new TransactionCreateDto
        {
            Amount = 100,
            CreatedAt = DateTime.Now,
            AccountId = 20, // Belongs to user 999
            CategoryId = 10 // Belongs to user 111, but account ownership should be checked first
        };

        var result = await controller.Create(incomingPayLoad);

        var badRequestResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Equal("Invalid account or category assignment.", badRequest.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryBelongsToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "malicious_user@example.com", Password = "abcd1234" }
            );

            seedContext.Category.Add(new Category { Id = 10, UserId = 999, Name = "Malicious Category" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new TransactionCreateDto
        {
            Amount = 100,
            AccountId = 20, // Belongs to user 111
            CategoryId = 10 // Belongs to user 999
        };

        var result = await controller.Create(incomingPayLoad);

        var badRequestResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        // This error comes from the TransactionController create method
        Assert.Equal("Invalid account or category assignment.", badRequest.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountDoesNotExist()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new TransactionCreateDto
        {
            Amount = 100,
            CreatedAt = DateTime.Now,
            AccountId = 999, // Non-existent account
            CategoryId = 1 // Belongs to user 111
        };

        var result = await controller.Create(incomingPayLoad);

        var badRequestResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        // This error comes from the TransactionController create method
        Assert.Equal("Invalid account or category assignment.", badRequest.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryDoesNotExist()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "User 111 Account" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new TransactionCreateDto
        {
            Amount = 100,
            CreatedAt = DateTime.Now,
            AccountId = 1, // Belongs to user 111
            CategoryId = 999 // Non-existent category
        };

        var result = await controller.Create(incomingPayLoad);

        var badRequestResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Equal("Invalid account or category assignment.", badRequest.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenAccountAndCategoryBelongToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "user999@example.com", Password = "abcd1234" }
                );
            seedContext.Category.Add(new Category { Id = 10, UserId = 999, Name = "Malicious Category" });
            seedContext.Account.Add(new Account { Id = 20, UserId = 999, AccountName = "Malicious Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new TransactionCreateDto
        {
            Amount = 100,
            CreatedAt = DateTime.Now,
            AccountId = 20, // Belongs to user 999
            CategoryId = 10 // Belongs to user 999
        };

        var result = await controller.Create(incomingPayLoad);

        var badRequestResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Equal("Invalid account or category assignment.", badRequest.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_Succeeds_ForValidUpdate()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new TransactionCreateDto
        {
            AccountId = 1,
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed
        };

        var result = await controller.Update(1, updatePayload);
        var noContentResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var noContent = Assert.IsType<NoContentResult>(noContentResult.Result);
        Assert.IsType<NoContentResult>(noContent);

        using var verifyContext = _context.CreateDbContext();
        var updatedTx = await verifyContext.Transaction.FindAsync(1);
        Assert.NotNull(updatedTx);
        Assert.Equal(20, updatedTx!.Amount);
        Assert.Equal("Updated Description", updatedTx.Description);
    }

    [Fact]
    public async Task Update_Fails_WhenTransactionIsVoided()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Voided });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new TransactionCreateDto
        {
            AccountId = 1,
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed,
            Account = new Account { Id = 1, UserId = 111, AccountName = "Checking" }
        };

        var result = await controller.Update(1, updatePayload);

        var badRequestResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Equal("Cannot update a voided transaction.", badRequest.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenAccountIsNull()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new TransactionCreateDto
        {
            AccountId = 999, // Non-existent account
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed
        };

        var result = await controller.Update(1, updatePayload);

        var badRequestResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Equal("Account not found.", badRequest.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenTransactionDoesNotExist()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new TransactionCreateDto
        {
            AccountId = 1,
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed
        };

        var result = await controller.Update(999, updatePayload);

        var notFoundResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var notFound = Assert.IsType<NotFoundResult>(notFoundResult.Result);
        Assert.IsType<NotFoundResult>(notFound);
    }

    [Fact]
    public async Task Update_Fails_WhenTransactionBelongsToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "user999@example.com", Password = "abcd1234" }
                );

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            
            seedContext.Account.Add(new Account { Id = 2, UserId = 999, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 2, UserId = 999, Name = "Car Repair" });
            seedContext.Transaction.Add(new Transaction { Id = 2, AccountId = 2, CategoryId = 2, Amount = 999, Description = "Hacked Transaction", Status = TransactionStatus.Completed });
            
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 999);

        var updatePayload = new TransactionCreateDto
        {
            AccountId = 1,
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed,
            Account = new Account { Id = 1, UserId = 999, AccountName = "Checking" }
        };

        var result = await controller.Update(1, updatePayload);

        var notFoundResult = Assert.IsType<ActionResult<TransactionResponseDto>>(result);
        var notFound = Assert.IsType<NotFoundResult>(notFoundResult.Result);
        Assert.IsType<NotFoundResult>(notFound);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_Succeeds_ForValidTransaction()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.Archive(1);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_ForNonExistentTransaction()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.Archive(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_ForTransactionBelongingToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Transaction.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 999); // Different user

        var result = await controller.Archive(1);
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

}