namespace PennySaver.API.Data;

public class PennySaverDbContext(DbContextOptions<PennySaverDbContext> options) 
    : DbContext(options)
{
    public DbSet<User> User { get; set; }
    public DbSet<UserInfo> UserInfo { get; set; }
    public DbSet<RefreshToken> RefreshToken { get; set; }
    public DbSet<Account> Account { get; set; }
    public DbSet<Budget> Budget { get; set; }
    public DbSet<Category> Category { get; set; }
    public DbSet<Transaction> Transaction { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>()
            .HasQueryFilter(a => a.DeletedAt == null);

        modelBuilder.Entity<Transaction>()
            .HasQueryFilter(t => t.Account!.DeletedAt == null);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany() 
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); 

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.CategoryId);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Account)
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.AccountId);

        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Budget>()
            .HasIndex(b => b.CategoryId);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.UserId);

        modelBuilder.Entity<Account>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Account>()
            .HasIndex(a => a.UserId);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.UserId);
    }
}