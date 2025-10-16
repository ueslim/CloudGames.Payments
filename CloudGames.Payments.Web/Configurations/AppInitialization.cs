using CloudGames.Payments.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CloudGames.Payments.Web.Configurations;

public static class AppInitialization
{
	public static async Task InitializeDatabasesAsync(this IHost app, CancellationToken ct = default)
	{
		using var scope = app.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
		try
		{
			var conn = db.Database.GetDbConnection();
			Serilog.Log.Information("[Startup] PaymentsDb connection: {DataSource}/{Database}", conn.DataSource, conn.Database);
			var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
			if (pending.Length > 0)
			{
				Serilog.Log.Information("[Startup] Pending migrations: {Migrations}", string.Join(", ", pending));
			}
			else
			{
				Serilog.Log.Information("[Startup] No pending migrations.");
			}
			await db.Database.MigrateAsync(ct);
			var applied = (await db.Database.GetAppliedMigrationsAsync(ct)).ToArray();
			Serilog.Log.Information("[Startup] EF Core migrations applied. Total: {Count}. Latest: {Latest}", applied.Length, applied.LastOrDefault());
		}
		catch
		{
			Serilog.Log.Warning("[Startup] EF Core migrations failed or not found. Falling back to EnsureCreated().");
			await db.Database.EnsureCreatedAsync(ct);
		}

		var eventsDb = scope.ServiceProvider.GetRequiredService<EventStoreSqlContext>();
		try
		{
			await eventsDb.Database.MigrateAsync(ct);
		}
		catch
		{
			await eventsDb.Database.EnsureCreatedAsync(ct);
		}
	}
}


