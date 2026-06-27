namespace PennySaver.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
    {
        private readonly IDbContextFactory<PennySaverDbContext> _context = dbContext;

        private int GetCurrentUserId() =>
            int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) 
            ? userId 
            : throw new UnauthorizedAccessException();

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            using var context = await _context.CreateDbContextAsync();
            var userAccounts = context.Accounts.Where(a => a.UserId == userId).ToList();
            return Ok(userAccounts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            int userId = GetCurrentUserId();
            using var context = await _context.CreateDbContextAsync();
            var account = context.Accounts.FirstOrDefault(a => a.Id == id && a.UserId == userId);
            if (account == null) return NotFound();
            return Ok(account);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Accounts account)
        {
            account.UserId = GetCurrentUserId();
            using var context = await _context.CreateDbContextAsync();
            context.Accounts.Add(account);
            await context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Accounts account)
        {
            if (id != account.Id) return BadRequest("ID mismatch.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            int userId = GetCurrentUserId();
            using var context = await _context.CreateDbContextAsync();
            
            var exists = await context.Accounts.AnyAsync(a => a.Id == id && a.UserId == userId);
            if (!exists) return NotFound();

            account.UserId = userId;
            context.Entry(account).State = EntityState.Modified;
            await context.SaveChangesAsync();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = GetCurrentUserId();
            using var dbContext = await _context.CreateDbContextAsync();
            var account = await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (account == null) return NotFound();

            dbContext.Accounts.Remove(account);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}