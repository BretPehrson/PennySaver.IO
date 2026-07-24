namespace PennySaver.Tests.Controllers;

public class EntityFrameworkControllerTests
{
    private readonly DbContextOptions<PennySaverDbContext> _options;
    private readonly string _connectionString;

    public EntityFrameworkControllerTests()
    {
        var testProjectDir = AppContext.BaseDirectory;

        var apiProjectDir = Path.Combine(testProjectDir, "..", "..", "..", "..", "PennySaver.API");
        apiProjectDir = Path.GetFullPath(apiProjectDir);

        var config = new ConfigurationBuilder()
            .SetBasePath(apiProjectDir)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<EntityFrameworkControllerTests>(optional: true) // For secure local dev tokens
            .Build();

        _connectionString = config.GetConnectionString("DefaultConnection")!;

        _options = new DbContextOptionsBuilder<PennySaverDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
    }

    [Fact]
    public void Database_Can_Successfully_Connect()
    {
        using var context = new PennySaverDbContext(_options);

        bool canConnect = context.Database.CanConnect();

        Assert.True(canConnect, $"Could not open a connection to the database.");
    }

    [Fact]
    public void Model_Should_Match_Latest_Migration_Snapshot()
    {
        using var context = new PennySaverDbContext(_options);

        bool hasPendingChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasPendingChanges, 
            "Your C# entity classes don't match your Migrations snapshot. Run 'dotnet ef migrations add <Name>'");
    }

    [Fact]
    public void Physical_Database_Should_Have_All_Migrations_Applied()
    {
        using var context = new PennySaverDbContext(_options);

        var pendingMigrations = context.Database.GetPendingMigrations();

        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public void Required_Seed_Data_Should_Exist_In_Database()
    {
        using var context = new PennySaverDbContext(_options);

        bool hasRoles = context.User.Any();

        Assert.True(hasRoles, "The database is missing critical seed data. Ensure migrations applied successfully.");
    }

    [Fact]
    public void Database_Schema_Should_Match_Model_Properties()
    {
        using var context = new PennySaverDbContext(_options);

        // Get all DbSet properties from the context
        var dbSetProperties = typeof(PennySaverDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && 
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        var errors = new List<string>();

        foreach (var dbSetProperty in dbSetProperties)
        {
            try
            {
                var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];
                var dbSet = dbSetProperty.GetValue(context);
                
                // Use reflection to call AsNoTracking().FirstOrDefault() on the DbSet
                var method = dbSet!
                    .GetType()
                    .GetMethod("FirstOrDefault", Type.EmptyTypes);

                var result = method?.Invoke(dbSet, null);
                
                // If we get here, the table exists and is queryable
            }
            catch (Exception ex)
            {
                var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0].Name;
                errors.Add($"{entityType} table/schema mismatch: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        Assert.True(errors.Count == 0, 
            $"The following entities have schema mismatches:\n{string.Join("\n", errors)}");
    }

    [Fact]
    public void All_Entities_Should_Have_Primary_Keys()
    {
        using var context = new PennySaverDbContext(_options);

        var model = context.Model;
        var entitiesWithoutPk = model.GetEntityTypes()
            .Where(e => e.FindPrimaryKey() == null)
            .Select(e => e.Name)
            .ToList();

        Assert.True(entitiesWithoutPk.Count == 0, 
            $"The following entities are missing primary keys: {string.Join(", ", entitiesWithoutPk)}");
    }

    [Fact]
    public void Soft_Delete_Query_Filters_Should_Apply()
    {
        using var context = new PennySaverDbContext(_options);

        // Account has a DeletedAt soft delete filter
        var deletedAccount = new Account 
        { 
            UserId = 1, 
            AccountName = "Deleted", 
            Type = 0,
            DeletedAt = DateTime.UtcNow 
        };
        
        context.Account.Add(deletedAccount);
        context.SaveChanges();

        // Query filters should exclude soft-deleted records
        var result = context.Account.FirstOrDefault(a => a.Id == deletedAccount.Id);

        Assert.True(result == null, "Query filters are not properly excluding soft-deleted records");
    }

    [Fact]
    public void SaveChanges_Actually_Persists_Data()
    {
        // Create and save in one context
        using (var context = new PennySaverDbContext(_options))
        {
            var newUser = new User 
            { 
                Email = "persist-test@example.com", 
                Password = "TestPassword123" 
            };
            context.User.Add(newUser);
            context.SaveChanges();
        }

        // Query in a fresh context to verify it persisted
        using (var context = new PennySaverDbContext(_options))
        {
            var savedUser = context.User.FirstOrDefault(u => u.Email == "persist-test@example.com");
            Assert.NotNull(savedUser);
            Assert.Equal("persist-test@example.com", savedUser.Email);
        }
    }

    [Fact]
    public void Required_Fields_Cannot_Be_Null()
    {
        using var context = new PennySaverDbContext(_options);

        // Verify that the Email column in the User table is NOT NULL
        var emailColumn = context.Model
            .FindEntityType(typeof(User))
            ?.FindProperty("Email");

        Assert.NotNull(emailColumn);
        Assert.False(emailColumn.IsNullable, "Email column should be NOT NULL in the database");
    }

    [Fact]
    public void Updates_Are_Properly_Saved()
    {
        int userId;

        // Create and save a user
        using (var context = new PennySaverDbContext(_options))
        {
            var newUser = new User 
            { 
                Email = "update-test@example.com", 
                Password = "OriginalPassword123" 
            };
            context.User.Add(newUser);
            context.SaveChanges();
            userId = newUser.UserId;
        }

        // Update the user in a new context
        using (var context = new PennySaverDbContext(_options))
        {
            var user = context.User.Find(userId);
            Assert.NotNull(user);
            user.Password = "UpdatedPassword123";
            context.SaveChanges();
        }

        // Verify the update persisted in a fresh context
        using (var context = new PennySaverDbContext(_options))
        {
            var updatedUser = context.User.Find(userId);
            Assert.NotNull(updatedUser);
            Assert.Equal("UpdatedPassword123", updatedUser.Password);
        }
    }

    [Fact]
    public void Concurrent_Context_Access_Should_Not_Interfere()
    {
        int userId1, userId2;

        // Create two users with separate contexts simultaneously
        using (var context1 = new PennySaverDbContext(_options))
        using (var context2 = new PennySaverDbContext(_options))
        {
            var user1 = new User 
            { 
                Email = "concurrent-user1@example.com", 
                Password = "Password123" 
            };
            var user2 = new User 
            { 
                Email = "concurrent-user2@example.com", 
                Password = "Password123" 
            };

            context1.User.Add(user1);
            context2.User.Add(user2);
            context1.SaveChanges();
            context2.SaveChanges();

            userId1 = user1.UserId;
            userId2 = user2.UserId;
        }

        // Verify both users were saved correctly
        using (var context = new PennySaverDbContext(_options))
        {
            var user1 = context.User.Find(userId1);
            var user2 = context.User.Find(userId2);

            Assert.NotNull(user1);
            Assert.NotNull(user2);
            Assert.Equal("concurrent-user1@example.com", user1.Email);
            Assert.Equal("concurrent-user2@example.com", user2.Email);
        }
    }
}