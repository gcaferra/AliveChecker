using AliveChecker.Application.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AliveChecker.Application.Database;

public class CheckerContext(DbContextOptions<CheckerContext> options) : DbContext(options)
{
    public DbSet<ImportedFile> Files => base.Set<ImportedFile>();
    public DbSet<Person> Peoples => base.Set<Person>();
    public DbSet<QueueItem> Queue => base.Set<QueueItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>().HasKey(p => p.Id);

        modelBuilder.Entity<QueueItem>().HasKey(p => p.Id);
        modelBuilder.Entity<QueueItem>().Property(p => p.TaxId).IsRequired();

        modelBuilder.Entity<ImportedFile>().HasKey(p => p.Id);

    }
}