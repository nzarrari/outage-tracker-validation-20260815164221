namespace OutageTracker.Core.Models;

public class Outage
{
    public Guid Id { get; set; }
    public string Region { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime ReportedAt { get; set; }
    public DateTime? RestoredAt { get; set; }
    public int Severity { get; set; }  // 1 (worst) – 5
}
