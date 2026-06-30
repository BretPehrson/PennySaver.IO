namespace PennySaver.Tests.Controllers;

public class CategoryControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;

    public CategoryControllerTests()
    {
        _context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    }

    private static CategoryController CreateController(IDbContextFactory<PennySaverDbContext> context, int? userId = null) => new(context)
    {
        ControllerContext = TestAuthHelper.GetControllerContext(userId)
    };

    [Fact]
    public async Task Create_ForcesOwnershipToLoggedInUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = CreateController(context, 111);

        var incomingPayLoad = new Category
        {
            Id = 1,
            CategoryName = "Groceries",
            UserId = 999 // Attempt to set UserId to a different value than the logged-in user
        };

        var result = await controller.Create(incomingPayLoad);
        Assert.IsType<CreatedAtActionResult>(result);

        // Verify that the category was created with the UserId of the logged-in user (111) and not the one in the payload (999)
        using var verifyContext = context.CreateDbContext();
        var createdCategory = await verifyContext.Category.FirstOrDefaultAsync(c => c.Id == 1);
        Assert.NotNull(createdCategory);
        Assert.Equal(111, createdCategory.UserId); // Should be 111, not 999
    }

    [Fact]
    public async Task Verify_Fail_WhenAddingSameCategoryTwiceForSameUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 111);

        var incomingPayLoad = new Category
        {
            Id = 2,
            CategoryName = "Groceries",
            UserId = 111
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Verify_Fail_WhenUserTriesToDeleteCategoryThatDoesNotBelongToThem()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 999);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Verify_Fail_WhenUserTriesToUpdateCategoryThatDoesNotBelongToThem()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 999);

        var incomingPayLoad = new Category
        {
            Id = 1,
            CategoryName = "Groceries Updated",
            UserId = 999
        };

        var result = await controller.Update(1, incomingPayLoad);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Verify_NewCategoryIsRetrievedByGetById()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 111);

        var result = await controller.GetById(1) as OkObjectResult;

        Assert.NotNull(result);
        var category = result.Value as Category;
        Assert.NotNull(category);
        Assert.Equal("Groceries", category.CategoryName);
    }

    [Fact]
    public async Task Verify_AllNewCategoriesAreRetrievedByGetAll()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            seedContext.Category.Add(new Category { Id = 2, UserId = 111, CategoryName = "Utilities" });
            seedContext.Category.Add(new Category { Id = 3, UserId = 999, CategoryName = "Entertainment" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 111);

        var result = await controller.GetAll() as OkObjectResult;

        Assert.NotNull(result);
        var categories = result.Value as List<Category>;
        Assert.NotNull(categories);
        Assert.Equal(2, categories.Count);

        // Change to user 999 and verify they only get their category
        controller = CreateController(context, 999);

        var resultForUser999 = await controller.GetAll() as OkObjectResult;

        Assert.NotNull(resultForUser999);
        var categoriesForUser999 = resultForUser999.Value as List<Category>;
        Assert.NotNull(categoriesForUser999);
        Assert.Single(categoriesForUser999);
    }

    [Fact]
    public async Task Verify_CategoryIsDeletedSuccessfully()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 111);

        var deleteResult = await controller.Delete(1);
        Assert.IsType<NoContentResult>(deleteResult);

        // Verify the category is actually deleted
        var getResult = await controller.GetById(1);
        Assert.IsType<NotFoundResult>(getResult);
    }

    [Fact]
    public async Task Verify_CategoryIsUpdatedSuccessfully()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 111);

        var updateResult = await controller.Update(1, new Category { Id = 1, UserId = 111, CategoryName = "Updated Groceries" });
        Assert.IsType<NoContentResult>(updateResult);

        // Verify the category is actually updated
        var getResult = await controller.GetById(1) as OkObjectResult;
        Assert.NotNull(getResult);
        var updatedCategory = getResult.Value as Category;
        Assert.NotNull(updatedCategory);
        Assert.Equal("Updated Groceries", updatedCategory.CategoryName);
    }

    [Fact]
    public async Task Verify_CategoryUpdateFails_WhenIdMismatch()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 111);

        var updateResult = await controller.Update(1, new Category { Id = 2, UserId = 111, CategoryName = "Updated Groceries" });
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(updateResult);
        
        Assert.Equal("ID mismatch.", badRequestResult.Value);
    }

    [Fact]
    public async Task Verify_CategoryUpdateFails_WhenModelStateIsInvalid()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 111);

        controller.ModelState.AddModelError("CategoryName", "Required");

        var updateResult = await controller.Update(1, new Category { Id = 1, UserId = 111, CategoryName = "" });
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(updateResult);
        
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_Fails_WhenCategoryBelongsToAnotherUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Category.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = CreateController(context, 111);

        var updateResult = await controller.Update(1, new Category { Id = 1, UserId = 999, CategoryName = "Updated Groceries" });
         var badRequestResult = Assert.IsType<BadRequestObjectResult>(updateResult);
        
        Assert.Equal("Cannot update a category that belongs to another user.", badRequestResult.Value);
    }
}