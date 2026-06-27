namespace PennySaver.Tests.Controllers;

public class AccountsControllerTests
{
    public static ControllerContext GetControllerContext(int userId)
    {
        var user = new List<Claim> 
        {
             new(JwtRegisteredClaimNames.Sub, userId.ToString()),
             new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        
        var identity = new ClaimsIdentity(user, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task Accounts_GetAll_ReturnsOnlyLoggedInUserAccounts()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        using (var seedContext = context.CreateDbContext())
        {
            seedContext.Accounts.AddRange(
                new Accounts { Id = 1, UserId = 111, AccountName = "User 1 Account" },
                new Accounts { Id = 2, UserId = 111, AccountName = "User 2 Account" },
                new Accounts { Id = 3, UserId = 999, AccountName = "Malicious Checking" }
            );

            await seedContext.SaveChangesAsync();
        }

        var controller = new AccountsController(context)
        {
            ControllerContext = GetControllerContext(111)
        };

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var accounts = Assert.IsType<List<Accounts>>(okResult.Value);

        Assert.Equal(2, accounts.Count);
        Assert.All(accounts, a => Assert.Equal(111, a.UserId));
    }

    [Fact]
    public async Task Accounts_Create_ForcesOwnershipToLoggedInUser()
    {
        var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var controller = new AccountsController(context)
        {
            ControllerContext = GetControllerContext(111)
        };

        var incomingPayLoad = new Accounts
        {
            Id = 5,
            AccountName = "New Account",
            UserId = 999 // Attempt to set to another user
        };

        var result = await controller.Create(incomingPayLoad);

        var createResult = Assert.IsType<CreatedAtActionResult>(result);
        var createdAccount = Assert.IsType<Accounts>(createResult.Value);

        Assert.Equal(111, createdAccount.UserId);
    }

    [Fact]
    public async Task Transactions_Create_Fails_WhenAccountBelongsToAnotherUser()
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
            ControllerContext = GetControllerContext(111)
        };

        var incomingPayLoad = new Transaction
        {
            Id = 1,
            Amount = 100,
            AccountId = 20, // Belongs to user 999
            CategoryId = 10 // Belongs to user 111, but account ownership should be checked first
        };

        var result = await controller.Create(incomingPayLoad);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}