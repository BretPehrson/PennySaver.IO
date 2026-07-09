namespace PennySaver.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController(IDbContextFactory<PennySaverDbContext> context) : ControllerBase
{
    private readonly IDbContextFactory<PennySaverDbContext> _context = context;

    private int GetCurrentUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) 
        ? userId 
        : throw new UnauthorizedAccessException();

    [HttpGet("overview")]
    public async Task<ActionResult<DashboardDto>> GetOverview()
    {
        var userId = GetCurrentUserId();
        using var context = await _context.CreateDbContextAsync();
        try
        {
            decimal totalCash = await context.Account
                .Where(a => a.UserId == userId)
                .SumAsync(a => a.Balance);

            decimal monthlyBudget = await context.Budget
                .Where(b => b.UserId == userId &&
                b.Month == DateTime.UtcNow.Month &&
                b.Year == DateTime.UtcNow.Year)
                .Select(b => b.TargetAmount)
                .FirstOrDefaultAsync();

            decimal totalSpentThisMonth = await context.Transaction
                .Join(context.Account, t => t.AccountId, a => a.Id, (t, a) => new { Transaction = t, Account = a })
                .Where(t => t.Account.UserId == userId &&
                t.Transaction.Date.Month == DateTime.UtcNow.Month &&
                t.Transaction.Date.Year == DateTime.UtcNow.Year)
                .SumAsync(t => t.Transaction.Amount);

            decimal remainingBudget = monthlyBudget - totalSpentThisMonth;

            var overview = new DashboardDto
            {
                TotalCash = totalCash,
                MonthlyBudget = monthlyBudget,
                RemainingBudget = remainingBudget
            };

            return Ok(overview);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while calculating your financial overview: {ex.Message}");
        }
    }

}