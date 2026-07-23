namespace PennySaver.Tests.Data;

public static class TestDbContextFactory
{
    public static IDbContextFactory<PennySaverDbContext> Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<PennySaverDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var context = new PennySaverDbContext(options))
        {
            context.Database.EnsureCreated();
        }

        return new MockDbContextFactory(options, connection);
    }

    private class MockDbContextFactory(DbContextOptions<PennySaverDbContext> options, SqliteConnection connection) : IDbContextFactory<PennySaverDbContext>
    {
        private readonly DbContextOptions<PennySaverDbContext> _options = options;
        private readonly SqliteConnection _connection = connection;

        public PennySaverDbContext CreateDbContext()
        {
            return new PennySaverDbContext(_options);
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}