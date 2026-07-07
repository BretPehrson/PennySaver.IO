using Going.Plaid;
using Going.Plaid.Accounts;

namespace PennySaver.API.Services;

public interface IPlaidClientWrapper
{
    Task<AccountsGetResponse> GetAccountBalancesAsync(string accessToken);
}

public class PlaidClientWrapper(PlaidClient plaidClient) : IPlaidClientWrapper
{
    private readonly PlaidClient _plaidClient = plaidClient;

    public async Task<AccountsGetResponse> GetAccountBalancesAsync(string accessToken) =>
         await _plaidClient.AccountsBalanceGetAsync(new() { AccessToken = accessToken });
}