using CloudGames.Payments.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CloudGames.Payments.Infra.Data;

public static class DatabaseInitializer
{
    public static async Task EnsureDatabaseMigratedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        
        // Initialize PaymentsDb
        await InitializePaymentsDbAsync(scope.ServiceProvider);
        
        // Initialize EventStoreDb
        await InitializeEventStoreDbAsync(scope.ServiceProvider);
    }

    private static async Task InitializePaymentsDbAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<PaymentsDbContext>();
        var creator = db.GetService<IRelationalDatabaseCreator>();

        if (!await creator.ExistsAsync())
        {
            Log.Warning("PaymentsDb não existe. Criando...");
            await creator.CreateAsync();
            await db.Database.MigrateAsync();
            Log.Information("PaymentsDb criado e migrations aplicadas");
            return;
        }

        var applied = await db.Database.GetAppliedMigrationsAsync();
        if (!applied.Any())
        {
            Log.Warning("PaymentsDb existe, mas nenhuma migration aplicada. Aplicando todas...");
            await db.Database.MigrateAsync();
            Log.Information("PaymentsDb migrations aplicadas");
            return;
        }

        var pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            Log.Information($"PaymentsDb: Aplicando {pending.Count()} migrations pendentes...");
            await db.Database.MigrateAsync();
            Log.Information("PaymentsDb: Migrations aplicadas com sucesso");
        }
        else
        {
            Log.Information("PaymentsDb: Banco atualizado, nenhuma migration pendente");
        }
    }

    private static async Task InitializeEventStoreDbAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<EventStoreSqlContext>();
        var creator = db.GetService<IRelationalDatabaseCreator>();

        if (!await creator.ExistsAsync())
        {
            Log.Warning("EventStoreDb não existe. Criando...");
            await creator.CreateAsync();
            await db.Database.MigrateAsync();
            Log.Information("EventStoreDb criado e migrations aplicadas");
            return;
        }

        var applied = await db.Database.GetAppliedMigrationsAsync();
        if (!applied.Any())
        {
            Log.Warning("EventStoreDb existe, mas nenhuma migration aplicada. Aplicando todas...");
            await db.Database.MigrateAsync();
            Log.Information("EventStoreDb migrations aplicadas");
            return;
        }

        var pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            Log.Information($"EventStoreDb: Aplicando {pending.Count()} migrations pendentes...");
            await db.Database.MigrateAsync();
            Log.Information("EventStoreDb: Migrations aplicadas com sucesso");
        }
        else
        {
            Log.Information("EventStoreDb: Banco atualizado, nenhuma migration pendente");
        }
    }
}

