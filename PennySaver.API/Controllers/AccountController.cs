namespace PennySaver.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
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
            var userAccounts = context.Account.Where(a => a.UserId == userId).ToList();
            return Ok(userAccounts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            int userId = GetCurrentUserId();
            using var context = await _context.CreateDbContextAsync();
            var account = context.Account.FirstOrDefault(a => a.Id == id && a.UserId == userId);
            if (account == null) return NotFound();
            return Ok(account);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Account account)
        {
            //Overwrite any provided UserId with the logged-in user's ID to enforce ownership
            account.UserId = GetCurrentUserId();
            using var context = await _context.CreateDbContextAsync();
            context.Account.Add(account);
            await context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Account account)
        {
            if (id != account.Id) return BadRequest("ID mismatch.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            int userId = GetCurrentUserId();
            using var context = await _context.CreateDbContextAsync();
            
            var exists = await context.Account.AnyAsync(a => a.Id == id && a.UserId == userId);
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
            using var context = await _context.CreateDbContextAsync();
            var account = await context.Account.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (account == null) return NotFound();

            context.Account.Remove(account);
            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}