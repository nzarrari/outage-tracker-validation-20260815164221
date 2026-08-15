using Microsoft.EntityFrameworkCore;
using OutageTracker.Core.Models;

namespace OutageTracker.Core;

public class OutageDbContext : DbContext
{
    public OutageDbContext(DbContextOptions<OutageDbContext> options) : base(options) { }

    public DbSet<Outage> Outages => Set<Outage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Outage>().HasData(
            new Outage { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Region = "East-01",  Description = "Transformer fire on Elm St", ReportedAt = new DateTime(2026, 8, 14, 20, 15, 0), Severity = 1 },
            new Outage { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Region = "West-04",  Description = "Downed line — tree contact",  ReportedAt = new DateTime(2026, 8, 14, 19, 40, 0), Severity = 2 },
            new Outage { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Region = "North-02", Description = "Substation breaker trip",     ReportedAt = new DateTime(2026, 8, 14, 18,  5, 0), RestoredAt = new DateTime(2026, 8, 14, 19, 30, 0), Severity = 3 }
        );
    }
}
