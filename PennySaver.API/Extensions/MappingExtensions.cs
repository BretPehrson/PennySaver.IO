namespace PennySaver.API.Extensions;

public static class MappingExtensions
{
    public static AccountResponseDto ToDto(this Account account) => new()
    {
        Id = account.Id,
        AccountName = account.AccountName,
        Institution = account.Institution,
        Type = account.Type,
        Balance = account.Balance,
        IsAutomated = account.IsAutomated,
        PlaidAccountId = account.PlaidAccountId,
        SyncStatus = account.SyncStatus
    };
}