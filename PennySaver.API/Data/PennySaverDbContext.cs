namespace PennySaver.API.Data;

public class PennySaverDbContext(DbContextOptions<PennySaverDbContext> options) 
    : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Accounts> Accounts { get; set; }
    public DbSet<Budgets> Budgets { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany() 
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); 

        modelBuilder.Entity<Transaction>()
            .HasOne(a => a.Account)
            .WithMany()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Budgets>()
            .HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}