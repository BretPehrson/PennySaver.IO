using Moq;

namespace PennySaver.Tests.Controllers;

public class PlaidControllerTests
{
    private readonly DbContextOptions<PennySaverDbContext> _dbOptions;
    private readonly Mock<IBankSyncService> _mockPlaidClient;

    public PlaidControllerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PennySaverDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockPlaidClient = new Mock<IBankSyncService>();
    }

    [Fact]
    public async Task RefreshUserBalancesAsync_ShouldSkipPlaidCall_WhenBalancesAreFresh()
    {
        using var context = new PennySaverDbContext(_dbOptions);
        var userId = 1;
        DateTime staleTime = DateTime.UtcNow.AddHours(-7);
        context.Account.Add(new Account
        {
            Id = 101,
            UserId = userId,
            IsAutomated = true,
            LastSynced = staleTime,
            PlaidAccessToken = "fake-access-token"
        });
        await context.SaveChangesAsync();

        var mockFactory = new Mock<IDbContextFactory<PennySaverDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(default))
            .ReturnsAsync(new PennySaverDbContext(_dbOptions));

        var coordinator = new AccountSyncCoordinator(mockFactory.Object, _mockPlaidClient.Object);

        await coordinator.RefreshUserBalancesAsync(userId);

        _mockPlaidClient.Verify(p => p.FetchLiveBalanceAsync("fake-access-token", It.IsAny<string>()), Times.Once);

        using var verifyContext = new PennySaverDbContext(_dbOptions);
        var account = await verifyContext.Account.FindAsync(101);

        Assert.NotNull(account);
        Assert.True(account.LastSynced > staleTime);
    }

    [Fact]
    public async Task Refresh_ShouldCallPlaid_WhenDataIsStale()
    {
        using var context = new PennySaverDbContext(_dbOptions);
        var userId = 1;
        DateTime staleTime = DateTime.UtcNow.AddHours(-7);
        context.Account.Add(new Account
        {
            Id = 101,
            UserId = userId,
            IsAutomated = true,
            LastSynced = staleTime,
            PlaidAccessToken = "fake-access-token"
        });
        await context.SaveChangesAsync();

        _mockPlaidClient.Setup(p => p.FetchLiveBalanceAsync("fake-access-token", It.IsAny<string>()));

        var mockFactory = new Mock<IDbContextFactory<PennySaverDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(default))
            .ReturnsAsync(new PennySaverDbContext(_dbOptions));

        var coordinator = new AccountSyncCoordinator(mockFactory.Object, _mockPlaidClient.Object);

        await coordinator.RefreshUserBalancesAsync(userId);

        _mockPlaidClient.Verify(p => p.FetchLiveBalanceAsync("fake-access-token", It.IsAny<string>()), Times.Once);

        using var verifyContext = new PennySaverDbContext(_dbOptions);
        var account = await verifyContext.Account.FindAsync(101);

        Assert.NotNull(account);
        Assert.True(account.LastSynced > staleTime);
    }
}