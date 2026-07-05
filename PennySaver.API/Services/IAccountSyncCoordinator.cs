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

                // 🛰️ Pull live numbers from interface contract
                var (liveBalance, _) = await _bankSyncService.FetchLiveBalanceAsync(account.PlaidAccessToken);
                
                account.Balance = liveBalance;
                account.SyncStatus = AccountSyncStatus.Healthy;
            }
            catch (Exception ex) when (ex.Message == "ITEM_LOGIN_REQUIRED")
            {
                // 🚨 Credentials revoked by user or bank -> Update row state flag
                account.SyncStatus = AccountSyncStatus.RequiresAttention;
            }
            catch (Exception)
            {
                // Generic connection blips/timeouts don't change SyncStatus
            }
        }

        await context.SaveChangesAsync();
    }
}