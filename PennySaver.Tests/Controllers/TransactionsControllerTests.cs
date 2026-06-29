namespace PennySaver.Tests.Controllers;

public class TransactionsControllerTests
{
    [Fact]
    public async Task Create_ForcesOwnershipToLoggedInUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());

        // Seed a category and account that belongs to our logged-in user (111)
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var incomingPayLoad = new Transaction
        {
            Id = 5,
            AccountId = 1,
            CategoryId = 1,
            Amount = 15,
            Description = "Lunch",
            Status = TransactionStatus.Completed
        };

        var result = await controller.Create(incomingPayLoad);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var createdTransaction = Assert.IsType<Transaction>(okResult.Value);

        Assert.Equal(1, createdTransaction.AccountId);
        Assert.Equal(1, createdTransaction.CategoryId);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithTransactions()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.AddRange(
                new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed },
                new Transaction { Id = 2, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Completed }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.GetAll(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var transactions = Assert.IsType<List<Transaction>>(okResult.Value);
        Assert.Equal(2, transactions.Count);
    }

    [Fact]
    public async Task GetAll_FiltersByStatusCompleted()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.AddRange(
                new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed },
                new Transaction { Id = 2, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Pending },
                new Transaction { Id = 3, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Voided }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.GetAll(TransactionStatus.Completed);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var transactions = Assert.IsType<List<Transaction>>(okResult.Value);
        Assert.Single(transactions);
        Assert.Equal(TransactionStatus.Completed, transactions[0].Status);
        Assert.Equal(1, transactions[0].Id);
    }

    [Fact]
    public async Task GetAll_FiltersByStatusPending()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.AddRange(
                new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed },
                new Transaction { Id = 2, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Pending },
                new Transaction { Id = 3, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Voided }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.GetAll(TransactionStatus.Pending);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var transactions = Assert.IsType<List<Transaction>>(okResult.Value);
        Assert.Single(transactions);
        Assert.Equal(TransactionStatus.Pending, transactions[0].Status);
        Assert.Equal(2, transactions[0].Id);
    }



    [Fact]
    public async Task GetAll_FiltersByStatusNotVoided()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.AddRange(
                new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed },
                new Transaction { Id = 2, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Pending },
                new Transaction { Id = 3, AccountId = 1, CategoryId = 1, Amount = 20, Description = "Dining Out", Status = TransactionStatus.Voided }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.GetAll(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var transactions = Assert.IsType<List<Transaction>>(okResult.Value);

        Assert.Equal(2, transactions.Count);
    }

    [Fact]
    public async Task GetById_ReturnsOkResult_WithTransaction()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.GetById(1);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var transaction = Assert.IsType<Transaction>(okResult.Value);

        Assert.Equal(1, transaction.Id);
        Assert.Equal(1, transaction.AccountId);
        Assert.Equal(1, transaction.CategoryId);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForNonExistentTransaction()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.GetById(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForTransactionBelongingToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(999) // Different user
        };

        var result = await controller.GetById(1);
        Assert.IsType<NotFoundResult>(result);
    }


    [Fact]
    public async Task Create_Fails_WhenAccountOrCategoryBelongsToAnotherUser()
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
            Description = "Hacked Transaction",
            Status = TransactionStatus.Completed,
            AccountId = 20, // Belongs to user 999
            CategoryId = 10 // Belongs to user 111
        };

        var result = await controller.Create(incomingPayLoad);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid account or category assignment.", badRequestResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenTransactionIsVoided()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Voided });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var updatePayload = new Transaction
        {
            Id = 1,
            AccountId = 1,
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed,
            Account = new Accounts { Id = 1, UserId = 111, AccountName = "Checking" }
        };

        var result = await controller.Update(1, updatePayload);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Cannot update a voided transaction.", badRequestResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenAccountIsNull()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var updatePayload = new Transaction
        {
            Id = 1,
            AccountId = 999, // Non-existent account
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed,
            Account = null // Account is null
        };

        var result = await controller.Update(1, updatePayload);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Account not found.", badRequestResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenTransactionDoesNotExist()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var updatePayload = new Transaction
        {
            Id = 999,
            AccountId = 1,
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed
        };

        var result = await controller.Update(999, updatePayload);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_Fails_WhenTransactionBelongsToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(999)
        };

        var updatePayload = new Transaction
        {
            Id = 1,
            AccountId = 1,
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed
        };

        var result = await controller.Update(1, updatePayload);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Cannot update a transaction that belongs to another user.", badRequestResult.Value);
    }

    [Fact]
    public async Task Update_Succeeds_ForValidUpdate()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var updatePayload = new Transaction
        {
            Id = 1,
            AccountId = 1,
            CategoryId = 1,
            Amount = 20,
            Description = "Updated Description",
            Status = TransactionStatus.Completed
        };

        var result = await controller.Update(1, updatePayload);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Archive_Succeeds_ForValidTransaction()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.Archive(1);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Archive_Fails_ForNonExistentTransaction()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.Archive(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Archive_Fails_ForTransactionBelongingToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.Add(new Accounts { Id = 1, UserId = 111, AccountName = "Checking" });
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Food" });
            seedContext.Transactions.Add(new Transaction { Id = 1, AccountId = 1, CategoryId = 1, Amount = 10, Description = "Groceries", Status = TransactionStatus.Completed });
            await seedContext.SaveChangesAsync();
        }

        var controller = new TransactionsController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(999) // Different user
        };

        var result = await controller.Archive(1);
        Assert.IsType<NotFoundResult>(result);
    }
}