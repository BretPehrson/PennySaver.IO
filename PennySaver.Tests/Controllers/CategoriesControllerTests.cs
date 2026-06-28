namespace PennySaver.Tests.Controllers;

public class CategoriesControllerTests
{
    [Fact]
    public async Task Verify_Fail_WhenAddingSameCategoryTwiceForSameUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new CategoriesController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

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
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new CategoriesController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(999)
        };

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Verify_Fail_WhenUserTriesToUpdateCategoryThatDoesNotBelongToThem()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new CategoriesController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(999)
        };

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
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new CategoriesController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

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
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            seedContext.Categories.Add(new Category { Id = 2, UserId = 111, CategoryName = "Utilities" });
            seedContext.Categories.Add(new Category { Id = 3, UserId = 999, CategoryName = "Entertainment" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new CategoriesController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var result = await controller.GetAll() as OkObjectResult;

        Assert.NotNull(result);
        var categories = result.Value as List<Category>;
        Assert.NotNull(categories);
        Assert.Equal(2, categories.Count);

        // Change to user 999 and verify they only get their category
        controller = new CategoriesController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(999)
        };

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
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new CategoriesController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

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
            seedContext.Categories.Add(new Category { Id = 1, UserId = 111, CategoryName = "Groceries" });
            await seedContext.SaveChangesAsync();
        }

        var controller = new CategoriesController(context)
        {
            ControllerContext = TestAuthHelper.GetControllerContext(111)
        };

        var updateResult = await controller.Update(1, new Category { Id = 1, UserId = 111, CategoryName = "Updated Groceries" });
        Assert.IsType<NoContentResult>(updateResult);

        // Verify the category is actually updated
        var getResult = await controller.GetById(1) as OkObjectResult;
        Assert.NotNull(getResult);
        var updatedCategory = getResult.Value as Category;
        Assert.NotNull(updatedCategory);
        Assert.Equal("Updated Groceries", updatedCategory.CategoryName);
    }
}