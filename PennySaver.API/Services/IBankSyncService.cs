namespace PennySaver.API.Services;

public interface IBankSyncService
{
    Task<(decimal Balance, string AccountId)> FetchLiveBalanceAsync(string accessToken);
}

public class MockBankSyncService : IBankSyncService
{
    public async Task<(decimal Balance, string AccountId)> FetchLiveBalanceAsync(string accessToken)
    {
        // Simulate API network latency over the wire
        await Task.Delay(800); 

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentException("Access token cannot be null or empty.");
        }

        // 🧪 Sandbox Trap Rule:
        // If the token contains the word "trigger_error", mimic a revoked bank credential error
        if (accessToken.Contains("trigger_error"))
        {
            throw new Exception("ITEM_LOGIN_REQUIRED");
        }

        var random = new Random(accessToken.GetHashCode());
        decimal simulatedBalance = (decimal)(random.Next(2500, 48000) + random.NextDouble());
        string simulatedAccountId = $"plaid_acc_id_{random.Next(10000, 99999)}";

        return (simulatedBalance, simulatedAccountId);
    }
}