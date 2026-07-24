namespace PennySaver.Tests.Controllers;

public class CategoryControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;

    public CategoryControllerTests()
    {
        _context = TestDbContextFactory.Create();
    }

    private static CategoryController CreateController(IDbContextFactory<PennySaverDbContext> context, int? userId = null) => new(context)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOnlyLoggedInUserCategories()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.AddRange(
                new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" },
                new User { UserId = 999, Email = "otheruser@example.com", Password = "abcd1234" }
            );
            
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Groceries" });
            seedContext.Category.Add(new Category { Id = 2, UserId = 111, Name = "Utilities" });
            seedContext.Category.Add(new Category { Id = 3, UserId = 999, Name = "Entertainment" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetAll();

        var actionResult = Assert.IsType<ActionResult<IEnumerable<CategoryResponseDto>>>(result);
        var categories = Assert.IsType<List<CategoryResponseDto>>(actionResult.Value);
        
        Assert.Equal(2, categories.Count);
        Assert.Contains(categories, c => c.Name == "Groceries");
        Assert.Contains(categories, c => c.Name == "Utilities");
        Assert.DoesNotContain(categories, c => c.Name == "Entertainment");
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyListWhenNoCategoriesForUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetAll();

        var actionResult = Assert.IsType<ActionResult<IEnumerable<CategoryResponseDto>>>(result);
        var categories = Assert.IsType<List<CategoryResponseDto>>(actionResult.Value);
        
        Assert.Empty(categories);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyListWhenUserDoesNotExist()
    {
        var controller = CreateController(_context, 123); // User 123 does not exist

        var result = await controller.GetAll();

        var actionResult = Assert.IsType<ActionResult<IEnumerable<CategoryResponseDto>>>(result);
        var categories = Assert.IsType<List<CategoryResponseDto>>(actionResult.Value);
        
        Assert.Empty(categories);
    }

    [Fact]
    public async Task GetAll_Fails_WhenUserIsUnauthorized()
    {
        var controller = CreateController(_context, null); // No user logged in

        var result = await controller.GetAll(); 

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task Verify_Succeeds_NewCategoryIsRetrievedByGetById()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetById(1);

        Assert.NotNull(result);
        var category = result.Value as CategoryResponseDto;
        Assert.NotNull(category);
        Assert.Equal("Groceries", category.Name);
    }

    [Fact]
    public async Task GetById_Fails_WhenCategoryDoesNotExist()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.GetById(999); // Non-existent category ID
        var actionResult = Assert.IsType<ActionResult<CategoryResponseDto>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetById_Fails_WhenCategoryBelongsToAnotherUser()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, Name = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 999); // Another user

        var result = await controller.GetById(1);
        var actionResult = Assert.IsType<ActionResult<CategoryResponseDto>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetById_Fails_WhenUserIsUnauthorized()
    {
        var controller = CreateController(_context, null); // No user logged in

        var result = await controller.GetById(1);
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_Succeeds_WhenUserIsAuthorized()
    {
        using (var seedContext = _context.CreateDbContext())
        {
            seedContext.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "New Category",
            Description = "This is a new category"
        };

        var result = await controller.Create(incomingPayLoad);

        var actionResult = Assert.IsType<ActionResult<CategoryResponseDto>>(result);
        var createdResult = Assert.IsType<CreatedAtRouteResult>(actionResult.Result);
        var createdCategory = Assert.IsType<CategoryResponseDto>(createdResult.Value);
        Assert.Equal("New Category", createdCategory.Name);
    }

    [Fact]
    public async Task Create_Fails_WhenUserIsUnauthorized()
    {
        var controller = CreateController(_context, null); // No user logged in

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "New Category",
            Description = "This is a new category"
        };

        var result = await controller.Create(incomingPayLoad);
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryNameExceedsMaxLength()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = new string('A', 101), // Exceeds max length of 100
            Description = "This is a new category"
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<CategoryResponseDto>>(result);
        var createResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Name cannot exceed 100 characters.", createResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryDescriptionExceedsMaxLength()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "New Category",
            Description = new string('A', 501) // Exceeds max length of 500
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<CategoryResponseDto>>(result);
        var createResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Description cannot exceed 500 characters.", createResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryIsMissing()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        CategoryCreateDto incomingPayLoad = null!;

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<CategoryResponseDto>>(result);
        var createResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Category data is required.", createResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryNameIsMissing()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "",
            Description = "This is a new category"
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<CategoryResponseDto>>(result);
        var createResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Name is required.", createResult.Value);
    }

    [Fact]
    public async Task Create_Fails_WhenCategoryAlreadyExists()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Existing Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "Existing Category",
            Description = "This category already exists"
        };

        var result = await controller.Create(incomingPayLoad);
        var actionResult = Assert.IsType<ActionResult<CategoryResponseDto>>(result);
        var createResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Category with the same name already exists for this user.", createResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_Succeeds_WhenCategoryIsUpdatedSuccessfully()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Old Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "Updated Category",
            Description = "This is an updated category"
        };

        var result = await controller.Update(1, incomingPayLoad);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_Fails_WhenCategoryDoesNotExist()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "Non-Existent Category",
            Description = "This category does not exist"
        };

        var result = await controller.Update(999, incomingPayLoad);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_Fails_WhenCategoryIsMissing()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        CategoryCreateDto incomingPayLoad = null!;

        var result = await controller.Update(1, incomingPayLoad);
        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        var createResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal("Category data is required.", createResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenCategoryNameIsMissing()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Old Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "",
            Description = "This is an updated category"
        };

        var result = await controller.Update(1, incomingPayLoad);
        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Name is required.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenNameIsTooLong()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Old Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = new string('A', 101),
            Description = "This is an updated category"
        };

        var result = await controller.Update(1, incomingPayLoad);
        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Name cannot exceed 100 characters.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenDescriptionIsTooLong()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Old Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "Updated Category",
            Description = new string('A', 501)
        };

        var result = await controller.Update(1, incomingPayLoad);
        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Description cannot exceed 500 characters.", actionResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenUserIsNotAuthorized()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Old Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, null); // No authenticated user

        var updatePayload = new CategoryCreateDto
        {
            Name = "Updated Category",
            Description = "This is an updated category"
        };

        var result = await controller.Update(1, updatePayload);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Update_Fails_WhenCategoryBelongsToOtherUser()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.User.Add(new User { UserId = 222, Email = "otheruser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Old Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 222);

        var incomingPayLoad = new CategoryCreateDto
        {
            Name = "Updated Category",
            Description = "This is an updated category"
        };

        var result = await controller.Update(1, incomingPayLoad);
        var actionResult = Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_Succeeds_WhenCategoryBelongsToUser()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Old Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.Delete(1);
        var actionResult = Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenCategoryBelongsToOtherUser()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.User.Add(new User { UserId = 222, Email = "otheruser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Old Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 222);

        var result = await controller.Delete(1);
        var actionResult = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenCategoryDoesNotExist()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, 111);

        var result = await controller.Delete(999);
        var actionResult = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Fails_WhenUserIsNotAuthorized()
    {
        using (var seedData = _context.CreateDbContext())
        {
            seedData.User.Add(new User { UserId = 111, Email = "testuser@example.com", Password = "abcd1234" });
            seedData.Category.Add(new Category { Id = 1, UserId = 111, Name = "Old Category" });
            await seedData.SaveChangesAsync();
        }

        var controller = CreateController(_context, null);

        var result = await controller.Delete(1);
        var actionResult = Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

}