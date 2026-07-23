namespace PennySaver.Tests.Controllers;

public class BudgetControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;

    public BudgetControllerTests()
    {
        _context = TestDbContextFactory.Create();
    }

    private static BudgetController CreateController(IDbContextFactory<PennySaverDbContext> context, int? userId = null) => new(context)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    #region GetAll Tests

    [Fact]
    public async Task Verify_GetAll()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "malicious_user@example.com", Password = "abcd1234" }
            );

            seedContext.Category.AddRange(
                new Category { Id = 1, UserId = 111, Name = "Food" },
                new Category { Id = 2, UserId = 111, Name = "Transport" },
                new Category { Id = 3, UserId = 999, Name = "Entertainment" }
            );
            seedContext.Budget.AddRange(
                new Budget { Id = 1, UserId = 111, TargetAmount = 50m, CategoryId = 1, Month = 1, Year = 2030 },
                new Budget { Id = 2, UserId = 111, TargetAmount = 100m, CategoryId = 2, Month = 1, Year = 2030 },
                new Budget { Id = 3, UserId = 999, TargetAmount = 200m, CategoryId = 3, Month = 1, Year = 2030 }
            );
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);
        var result = await controller.GetAll();
        var actionResult = Assert.IsType<ActionResult<IEnumerable<BudgetResponseDto>>>(result);
        var budgets = Assert.IsType<List<BudgetResponseDto>>(actionResult.Value);
        Assert.Equal(2, budgets.Count);
        Assert.Equal(1, budgets.First(b => b.CategoryId == 1).Id);
        Assert.Equal(50m, budgets.First(b => b.CategoryId == 1).TargetAmount);
        Assert.Equal(2, budgets.First(b => b.CategoryId == 2).Id);
        Assert.Equal(100m, budgets.First(b => b.CategoryId == 2).TargetAmount);

        //Get user 999's budgets
        controller = CreateController(_context, 999);
        result = await controller.GetAll();
        actionResult = Assert.IsType<ActionResult<IEnumerable<BudgetResponseDto>>>(result);
        var budgets1 = Assert.IsType<List<BudgetResponseDto>>(actionResult.Value);
        Assert.Single(budgets1);
        Assert.Equal(3, budgets1.First().Id);
        Assert.Equal(200m, budgets1.First().TargetAmount);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoBudgetsExistForUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);
        var result = await controller.GetAll();
        var actionResult = Assert.IsType<ActionResult<IEnumerable<BudgetResponseDto>>>(result);
        var budgets = Assert.IsType<List<BudgetResponseDto>>(actionResult.Value);
        Assert.Empty(budgets);
    }

    [Fact]
    public async Task GetAll_Fails_WhenUserIdClaimIsMissing()
    {
        var controller = CreateController(_context, null);
        var result = await controller.GetAll();
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_Succeeds_WhenExistsAndBelongsToUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, CategoryId = 1, TargetAmount = 50, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);
        var result = await controller.GetById(1);
        var actionResult = Assert.IsType<ActionResult<BudgetResponseDto>>(result);
        var budget = Assert.IsType<BudgetResponseDto>(actionResult.Value);

        Assert.Equal(1, budget.Id);
        Assert.Equal(50m, budget.TargetAmount);
        Assert.Equal(1, budget.CategoryId);
        Assert.Equal(1, budget.Month);
        Assert.Equal(2030, budget.Year);
    }

    [Fact]
    public async Task GetById_Fails_WhenDoesNotExist()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, CategoryId = 1, TargetAmount = 50, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);
        var result = await controller.GetById(2);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_Fails_WhenDoesNotBelongToUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.User.Add(new User { UserId = 222, Email = "user222@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, CategoryId = 1, TargetAmount = 50, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 222);
        var result = await controller.GetById(1);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_Succeeds_WhenValidDataIsProvided()
    {
        // Seed a category that belongs to our logged-in user (111)
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new BudgetCreateDto
        {
            Name = "New Budget",
            TargetAmount = 150,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Create(incomingPayLoad);

        var actionResult = Assert.IsType<ActionResult<BudgetResponseDto>>(result);
        var createResult = Assert.IsType<CreatedAtRouteResult>(actionResult.Result);
        var createdBudget = Assert.IsType<BudgetResponseDto>(createResult.Value);
        Assert.Equal("New Budget", createdBudget.Name);
        Assert.Equal(150, createdBudget.TargetAmount);
        Assert.Equal(1, createdBudget.CategoryId);
        Assert.Equal(1, createdBudget.Month);
        Assert.Equal(2030, createdBudget.Year);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryDoesNotBelongToUser()
    {
        // Seed a category that belongs to a different user (999)
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.User.Add(new User { UserId = 999, Email = "user999@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 999, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new BudgetCreateDto
        {
            Name = "New Budget",
            TargetAmount = 150,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<BudgetResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Invalid category assignment.", badRequestResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenNameIsMissing()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new BudgetCreateDto
        {
            TargetAmount = 150,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<BudgetResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Name is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenTargetAmountIsNegative()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new BudgetCreateDto
        {
            Name = "New Budget",
            TargetAmount = -50,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<BudgetResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Target amount must be non-negative.", badRequestResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenMonthIsInvalid()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new BudgetCreateDto
        {
            Name = "New Budget",
            TargetAmount = 150,
            CategoryId = 1,
            Month = 13, // Invalid month
            Year = 2030
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<BudgetResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Month must be between 1 and 12.", badRequestResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenYearIsInThePast()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new BudgetCreateDto
        {
            Name = "New Budget",
            TargetAmount = 150,
            CategoryId = 1,
            Month = 1,
            Year = DateTime.Now.Year - 1 // Invalid year
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<BudgetResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Year must be the current year or later.", badRequestResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenNameIsTooLong()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new BudgetCreateDto
        {
            Name = new string('A', 101), // Name exceeds 100 characters
            TargetAmount = 150,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<BudgetResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Name cannot exceed 100 characters.", badRequestResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenBudgetAlreadyExists()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "New Budget", TargetAmount = 150, CategoryId = 1, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new BudgetCreateDto
        {
            Name = "New Budget",
            TargetAmount = 150,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<BudgetResponseDto>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("A budget with this name already exists.", badRequestResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Verify_Success_UpdateCategory()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = "Updated Budget",
            TargetAmount = 75,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(1, updatePayload);
        Assert.IsType<NoContentResult>(result);

        using var verifyContext = _context.CreateDbContext();
        var updatedBudget = await verifyContext.Budget.FindAsync(1);
        Assert.NotNull(updatedBudget);
        Assert.Equal(75, updatedBudget.TargetAmount);
        Assert.Equal(1, updatedBudget.CategoryId);
    }

    [Fact]
    public async Task Update_Fails_WhenBudgetDoesNotExist()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = "Updated Budget",
            TargetAmount = 75,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(111, updatePayload);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_Fails_WhenNameIsTooLong()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = new string('A', 101), // Name exceeds 100 characters
            TargetAmount = 75,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(1, updatePayload);

        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Name cannot exceed 100 characters.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenTargetAmountIsNegative()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = "Updated Budget",
            TargetAmount = -10, // Negative target amount
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(1, updatePayload);

        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Target amount must be non-negative.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenNameIsRequired()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = "", // Name is required
            TargetAmount = 75,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(1, updatePayload);

        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Name is required.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenCategoryIdIsInvalid()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = "Updated Budget",
            TargetAmount = 75,
            CategoryId = 999, // Invalid category ID
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(1, updatePayload);

        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid category assignment.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenCategoryIdInvalidFormat()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = "Updated Budget",
            TargetAmount = 75,
            CategoryId = -1, // Non-existent category ID
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(1, updatePayload);

        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Valid category ID is required.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenCategoryDoesNotBelongToUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.User.Add(new User { UserId = 999, Email = "user999@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Category.Add(new Category { Id = 2, UserId = 999, Name = "Transport" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = "Updated Budget",
            TargetAmount = 75,
            CategoryId = 2, // Category belongs to another user
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(1, updatePayload);

        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid category assignment.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenBudgetIsNull()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.Update(1, null!);

        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Budget data is required.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenStartDateIsAfterEndDate()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = "Updated Budget",
            TargetAmount = 75,
            CategoryId = 1,
            Month = 2,
            Year = 2030,
            StartDate = new DateTime(2030, 2, 1),
            EndDate = new DateTime(2030, 1, 1)
        };

        var result = await controller.Update(1, updatePayload);

        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Start date must be before end date.", actionResult.Value);
    }

    [Fact]
    public async Task Update_NotFoundForOwner()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            Name = "Updated Budget",
            TargetAmount = 75,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(999, updatePayload);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_BadRequestForIdMismatch()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, Name = "Initial Budget", TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var updatePayload = new BudgetCreateDto
        {
            TargetAmount = 75,
            CategoryId = 1,
            Month = 1,
            Year = 2030
        };

        var result = await controller.Update(1, updatePayload);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_RemovesBudget()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 111, TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.Delete(1);
        Assert.IsType<NoContentResult>(result);

        // Verify it's actually deleted
        using var verifyContext = _context.CreateDbContext();
        var budget = await verifyContext.Budget.FindAsync(1);
        Assert.False(budget!.IsActive);
    }

    [Fact]
    public async Task Delete_NotFoundForNonExistentBudget()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "user111@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.Delete(999); // Non-existent budget ID
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_NotFoundForOtherUsersBudget()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 999, Email = "user999@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 999, Name = "Food" });
            seedContext.Budget.Add(new Budget { Id = 1, UserId = 999, TargetAmount = 50, CategoryId = 1, Month = 1, Year = 2030 });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111); // Logged in as a different user

        var result = await controller.Delete(1); // Attempt to delete another user's budget
        Assert.IsType<NotFoundResult>(result);
    }
    
    #endregion
}