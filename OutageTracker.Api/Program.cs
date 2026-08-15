using Microsoft.EntityFrameworkCore;
using OutageTracker.Core;
using OutageTracker.Core.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OutageDbContext>(o =>
    o.UseSqlite("Data Source=../outages.db"));
builder.Services.AddScoped<OutageService>();
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OutageDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/outages", async (int? severity, OutageService svc) =>
    await svc.GetBySeverityAsync(severity));
app.MapGet("/outages/{id:guid}", async (Guid id, OutageService svc) =>
    await svc.GetByIdAsync(id) is Outage o ? Results.Ok(o) : Results.NotFound());
app.MapPost("/outages", async (Outage outage, OutageService svc) =>
{
    var created = await svc.CreateAsync(outage);
    return Results.Created($"/outages/{created.Id}", created);
});

app.Run();
