using Microsoft.EntityFrameworkCore;
using OutageTracker.Core.Models;

namespace OutageTracker.Core;

public class OutageService
{
    private readonly OutageDbContext _db;

    public OutageService(OutageDbContext db) => _db = db;

    public async Task<List<Outage>> GetAllAsync() =>
        await _db.Outages.OrderByDescending(o => o.ReportedAt).ToListAsync();

    /// <summary>
    /// Returns outages filtered by severity, or all outages when <paramref name="severity"/> is null.
    /// Results are ordered by <see cref="Outage.ReportedAt"/> descending.
    /// </summary>
    /// <param name="severity">Severity level 1–5, or null to return all.</param>
    public async Task<List<Outage>> GetBySeverityAsync(int? severity) =>
        await _db.Outages
            .Where(o => severity == null || o.Severity == severity)
            .OrderByDescending(o => o.ReportedAt)
            .ToListAsync();

    public async Task<Outage?> GetByIdAsync(Guid id) =>
        await _db.Outages.FindAsync(id);

    public async Task<Outage> CreateAsync(Outage outage)
    {
        outage.Id = Guid.NewGuid();
        if (outage.ReportedAt == default) outage.ReportedAt = DateTime.UtcNow;
        _db.Outages.Add(outage);
        await _db.SaveChangesAsync();
        return outage;
    }
}
