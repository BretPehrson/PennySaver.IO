namespace PennySaver.API.Services;

public interface IAccountSyncCoordinator
{
    Task RefreshUserBalancesAsync(int userId);
}

public class AccountSyncCoordinator(
        IDbContextFactory<PennySaverDbContext> contextFactory, 
        IBankSyncService bankSyncService) 
        : IAccountSyncCoordinator
{
    private readonly IDbContextFactory<PennySaverDbContext> _contextFactory = contextFactory;
    private readonly IBankSyncService _bankSyncService = bankSyncService;

    public async Task RefreshUserBalancesAsync(int userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var sixHoursAgo = DateTime.UtcNow.AddHours(-6);
        var needsSync = await context.Account
            .AnyAsync(a => a.UserId == userId && a.IsAutomated && a.LastSynced < sixHoursAgo);
        if (!needsSync) return;

        var automatedAccounts = await context.Account
            .Where(a => a.UserId == userId && a.IsAutomated)
            .ToListAsync();

        if (automatedAccounts.Count == 0) return;

        foreach (var account in automatedAccounts)
        {
            try
            {
                if (string.IsNullOrEmpty(account.PlaidAccessToken))
                {
                    account.SyncStatus = AccountSyncStatus.RequiresAttention;
                    continue;
                }

                // Pull live numbers from interface contract
                var (liveBalance, _) = await _bankSyncService.FetchLiveBalanceAsync(account.PlaidAccessToken, account.PlaidAccountId!);
                account.Balance = liveBalance;
                account.SyncStatus = AccountSyncStatus.Healthy;
            }
            catch (Exception ex) when (ex.Message == "ITEM_LOGIN_REQUIRED")
            {
                account.SyncStatus = AccountSyncStatus.RequiresAttention;
            }
            catch (Exception) { }
            finally
            {
                account.LastSynced = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
    }
}