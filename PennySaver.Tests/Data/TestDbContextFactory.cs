namespace PennySaver.Tests.Data;

public static class TestDbContextFactory
{
    public static IDbContextFactory<PennySaverDbContext> Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<PennySaverDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new MockDbContextFactory(options);
    }

    private class MockDbContextFactory(DbContextOptions<PennySaverDbContext> options) : IDbContextFactory<PennySaverDbContext>
    {
        private readonly DbContextOptions<PennySaverDbContext> _options = options;

        public PennySaverDbContext CreateDbContext()
        {
            return new PennySaverDbContext(_options);
        }
    }
}