using AliveChecker.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace AliveChecker.Application.Tests;

public class DatabaseFixture: IDisposable
{
    public DbContextOptions<CheckerContext> Options { get; }
    public CheckerContext Context { get; }

    public DatabaseFixture()
    {
        var dbPath = Path.GetTempFileName();
        var optionsBuilder = new DbContextOptionsBuilder<CheckerContext>()
            .UseSqlite($"Data Source={dbPath}");
        Options = optionsBuilder.Options;
        Context = new CheckerContext(Options);
        Context.Database.EnsureCreated();
    }
    public void Dispose()
    {
        Context.Database.EnsureDeleted();
    }
}