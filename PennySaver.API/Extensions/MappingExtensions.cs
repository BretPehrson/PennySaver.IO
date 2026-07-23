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

    public static BudgetResponseDto ToDto(this Budget budget) => new()
    {
        Id = budget.Id,
        CategoryId = budget.CategoryId,
        Name = budget.Name,
        TargetAmount = budget.TargetAmount,
        StartDate = budget.StartDate,
        EndDate = budget.EndDate,
        Month = budget.Month,
        Year = budget.Year
    };

    public static CategoryResponseDto ToDto(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name
    };

    public static TransactionResponseDto ToDto(this Transaction transaction) => new()
    {
        Id = transaction.Id,
        Amount = transaction.Amount,
        Description = transaction.Description,
        CreatedAt = transaction.CreatedAt,
        Status = transaction.Status,
        AccountId = transaction.AccountId,
        CategoryId = transaction.CategoryId
    };
}