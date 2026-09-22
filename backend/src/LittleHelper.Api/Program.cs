using LittleHelper.Domain.CycleTracking;
using LittleHelper.Domain.Messaging;
using LittleHelper.Infrastructure.DependencyInjection;
using LittleHelper.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=data/littlehelper.db";

// SQLite won't create a missing parent directory itself — matters on a fresh
// Compute Engine VM where data/ doesn't exist until the app creates it.
var dbDirectory = Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource);
if (!string.IsNullOrEmpty(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<CycleTrackingService>();
builder.Services.AddScoped<ICommandHandler, CycleTrackingCommandHandler>();
builder.Services.AddScoped<MessageHandler>();
builder.Services.AddControllers();

var app = builder.Build();

// SQLite file lives on the Compute Engine VM's persistent disk in prod (see
// docs/analysis-phase.md's hosting revision) — Migrate() creates it on first
// run rather than requiring a manual migration step as part of deploy.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LittleHelperDbContext>();
    db.Database.Migrate();
}

app.MapControllers();

app.Run();
