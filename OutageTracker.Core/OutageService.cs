using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
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
    // FIXED — parameterized query prevents SQL injection; uses injected DbContext connection.
    public async Task<List<string>> FindByRegionRawAsync(string region)
    {
        var results = new List<string>();
        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Description FROM Outages WHERE Region = $region";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$region";
        parameter.Value = region;
        command.Parameters.Add(parameter);
        using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            results.Add(reader.GetString(0));
        }
        return results;
    }
}