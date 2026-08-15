using Microsoft.EntityFrameworkCore;
using OutageTracker.Core.Models;

namespace OutageTracker.Core;

public class OutageService
{
    private readonly OutageDbContext _db;

    public OutageService(OutageDbContext db) => _db = db;

    public async Task<List<Outage>> GetAllAsync() =>
        await _db.Outages.OrderByDescending(o => o.ReportedAt).ToListAsync();

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
