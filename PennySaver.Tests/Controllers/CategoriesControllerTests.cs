namespace PennySaver.Tests.Controllers;

public class CategoriesControllerTests
{
    [Fact]
    public async Task Verify_Fail_When_Adding_Same_Category_Twice_For_Same_User()
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
    public async Task Verify_Fail_When_User_Tries_To_Delete_Category_That_Does_Not_Belong_To_Them()
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
}