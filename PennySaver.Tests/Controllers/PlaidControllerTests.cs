using Moq;

namespace PennySaver.Tests.Controllers;

public class PlaidControllerTests
{
    private readonly IDbContextFactory<PennySaverDbContext> _context;
    private readonly Mock<IBankSyncService> _mockPlaidClient;

    public PlaidControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _mockPlaidClient = new Mock<IBankSyncService>();
    }

    [Fact]
    public async Task RefreshUserBalancesAsync_ShouldSkipPlaidCall_WhenBalancesAreFresh()
    {
        var freshTime = DateTime.UtcNow.AddMinutes(-7);
        int userId = 101;
        using (var seedContext = await _context.CreateDbContextAsync())
        {
            seedContext.User.Add(new User { UserId = userId, Email = "user101@example.com", Password = "abcd1234" });
            seedContext.Account.Add(new Account
            {
                Id = 1,
                UserId = userId,
                IsAutomated = true,
                LastSynced = freshTime,
                PlaidAccessToken = "fake-access-token"
            });
            await seedContext.SaveChangesAsync();
        }

        var coordinator = new AccountSyncCoordinator(_context, _mockPlaidClient.Object);

        await coordinator.RefreshUserBalancesAsync(userId);

        _mockPlaidClient.Verify(p => p.FetchLiveBalanceAsync("fake-access-token", It.IsAny<string>()), Times.Never);

        using var verifyContext = await _context.CreateDbContextAsync();
        var account = await verifyContext.Account.FindAsync(1);

        Assert.NotNull(account);
        Assert.Equal(freshTime, account.LastSynced);
    }

    [Fact]
    public async Task Refresh_ShouldCallPlaid_WhenDataIsStale()
    {
        var userId = 1;
        var staleTime = DateTime.UtcNow.AddDays(-1);
        using (var seedContext = await _context.CreateDbContextAsync())
        {
            seedContext.User.Add(new User { UserId = userId, Email = "user1@example.com", Password = "abcd1234" });

            seedContext.Account.Add(new Account
            {
                Id = 102,
                UserId = userId,
                IsAutomated = true,
                LastSynced = staleTime,
                PlaidAccessToken = "fake-access-token"
            });
            await seedContext.SaveChangesAsync();
        }

        _mockPlaidClient.Setup(p => p.FetchLiveBalanceAsync("fake-access-token", It.IsAny<string>()))
            .ReturnsAsync((150.75m, "mock-institution-id"));

        var coordinator = new AccountSyncCoordinator(_context, _mockPlaidClient.Object);

        await coordinator.RefreshUserBalancesAsync(userId);

        _mockPlaidClient.Verify(p => p.FetchLiveBalanceAsync("fake-access-token", It.IsAny<string>()), Times.Once);

        using var verifyContext = await _context.CreateDbContextAsync();
        var account = await verifyContext.Account.FindAsync(102);

        Assert.NotNull(account);
        Assert.True(account.LastSynced > staleTime);
    }
}