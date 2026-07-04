namespace PennySaver.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(IDbContextFactory<PennySaverDbContext> dbContext) : ControllerBase
    {
        private readonly IDbContextFactory<PennySaverDbContext> _context = dbContext;

        private int GetCurrentUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) 
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
        public async Task<IActionResult> Create([FromBody] AccountCreateDto dto)
        {
            if (dto == null) return BadRequest("Account data is required.");
            if (dto.AccountName == null || dto.AccountName.Trim() == "") return BadRequest("The AccountName field is required.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.Balance < 0) return BadRequest("Balance cannot be negative.");
            if (dto.Institution != null && dto.Institution.Length > 100) return BadRequest("Institution name cannot exceed 100 characters.");

            //Overwrite any provided UserId with the logged-in user's ID to enforce ownership
            var newAccount = new Account
            {
                AccountName = dto.AccountName,
                Institution = dto.Institution!,
                Type = dto.Type,
                Balance = dto.Balance,
                UserId = GetCurrentUserId()
            };
            using var context = await _context.CreateDbContextAsync();
            context.Account.Add(newAccount);
            await context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetById), new { id = newAccount.Id }, newAccount);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AccountCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.AccountName == null || dto.AccountName.Trim() == "") return BadRequest("The AccountName field is required.");
            if (dto.Balance < 0) return BadRequest("Balance cannot be negative.");
            if (dto.Institution != null && dto.Institution.Length > 100) return BadRequest("Institution name cannot exceed 100 characters.");

            int userId = GetCurrentUserId();
            using var context = await _context.CreateDbContextAsync();
            
            var exists = await context.Account.AnyAsync(a => a.Id == id && a.UserId == userId);
            if (!exists) return NotFound();

            var accountToUpdate = await context.Account.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (accountToUpdate == null) return NotFound();

            accountToUpdate.AccountName = dto.AccountName;
            accountToUpdate.Institution = dto.Institution!;
            accountToUpdate.Type = dto.Type;
            accountToUpdate.Balance = dto.Balance;

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