using Microsoft.EntityFrameworkCore;
using OutageTracker.Core;
using OutageTracker.Core.Models;

namespace OutageTracker.Tests;

public class OutageServiceTests
{
    private static OutageDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<OutageDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new OutageDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task GetBySeverityAsync_NullSeverity_ReturnsAllOutages()
    {
        using var db = CreateDb(nameof(GetBySeverityAsync_NullSeverity_ReturnsAllOutages));
        var svc = new OutageService(db);

        var result = await svc.GetBySeverityAsync(null);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetBySeverityAsync_ExactMatch_ReturnsFilteredOutages()
    {
        using var db = CreateDb(nameof(GetBySeverityAsync_ExactMatch_ReturnsFilteredOutages));
        var svc = new OutageService(db);

        var result = await svc.GetBySeverityAsync(1);

        Assert.Single(result);
        Assert.Equal("East-01", result[0].Region);
    }

    [Fact]
    public async Task GetBySeverityAsync_OutOfRange_ReturnsEmpty()
    {
        using var db = CreateDb(nameof(GetBySeverityAsync_OutOfRange_ReturnsEmpty));
        var svc = new OutageService(db);

        var result = await svc.GetBySeverityAsync(99);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBySeverityAsync_NullSeverity_ReturnsResultsOrderedByReportedAtDescending()
    {
        using var db = CreateDb(nameof(GetBySeverityAsync_NullSeverity_ReturnsResultsOrderedByReportedAtDescending));
        var svc = new OutageService(db);

        var result = await svc.GetBySeverityAsync(null);

        for (int i = 0; i < result.Count - 1; i++)
            Assert.True(result[i].ReportedAt >= result[i + 1].ReportedAt);
    }

    [Fact]
    public async Task CreateAsync_WithEstimatedRestorationAt_PersistsValue()
    {
        using var db = CreateDb(nameof(CreateAsync_WithEstimatedRestorationAt_PersistsValue));
        var svc = new OutageService(db);
        var eta = new DateTime(2026, 8, 15, 12, 45, 0, DateTimeKind.Utc);

        var created = await svc.CreateAsync(new Outage
        {
            Region = "South-01",
            Description = "Pole failure",
            Severity = 2,
            EstimatedRestorationAt = eta
        });

        var saved = await svc.GetByIdAsync(created.Id);

        Assert.NotNull(saved);
        Assert.Equal(eta, saved.EstimatedRestorationAt);
    }

    [Fact]
    public async Task GetBySeverityAsync_ReturnsEstimatedRestorationAtFromSeededOutage()
    {
        using var db = CreateDb(nameof(GetBySeverityAsync_ReturnsEstimatedRestorationAtFromSeededOutage));
        var svc = new OutageService(db);

        var result = await svc.GetBySeverityAsync(1);

        var outage = Assert.Single(result);
        Assert.Equal(new DateTime(2026, 8, 14, 22, 0, 0), outage.EstimatedRestorationAt);
    }
}
