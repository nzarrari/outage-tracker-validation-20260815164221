using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OutageTracker.Core;
using OutageTracker.Core.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OutageDbContext>(o =>
    o.UseSqlite("Data Source=../outages.db"));
builder.Services.AddScoped<OutageService>();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("by-region", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OutageDbContext>();
    db.Database.EnsureCreated();
}

app.UseRateLimiter();

app.MapGet("/outages", async (int? severity, OutageService svc) =>
    await svc.GetBySeverityAsync(severity));
app.MapGet("/outages/{id:guid}", async (Guid id, OutageService svc) =>
    await svc.GetByIdAsync(id) is Outage o ? Results.Ok(o) : Results.NotFound());
app.MapPost("/outages", async (Outage outage, OutageService svc) =>
{
    var created = await svc.CreateAsync(outage);
    return Results.Created($"/outages/{created.Id}", created);
});
app.MapGet("/outages/by-region", async (string region, OutageService svc) =>
    await svc.FindByRegionRawAsync(region))
    .RequireRateLimiting("by-region");

app.Run();

public partial class Program;
