using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Data;

namespace RepairFlow.Api.Extensions;

public static class DatabaseInitializer
{
    /// <summary>
    /// Приводит базу в рабочее состояние на старте: применяет миграции и, если включён демо-режим,
    /// заполняет её данными. Благодаря этому «docker compose up» действительно достаточно.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IHost app, CancellationToken ct = default)
    {
        using var scope = app.Services.CreateScope();

        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var db = services.GetRequiredService<AppDbContext>();
        var demo = services.GetRequiredService<IOptions<DemoOptions>>().Value;

        await WaitForDatabaseAsync(db, logger, ct);

        if (db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync(ct);
            logger.LogInformation("Миграции применены.");
        }
        else
        {
            // Запасной путь для свежего клона, в котором миграции ещё не сгенерированы:
            // схема создаётся напрямую, чтобы демо поднималось одной командой.
            await db.Database.EnsureCreatedAsync(ct);
            logger.LogWarning("Миграции не найдены — схема создана через EnsureCreated.");
        }

        if (demo.SeedOnStartup)
        {
            await DbSeeder.SeedAsync(db, demo, logger, ct);
        }
    }

    /// <summary>В docker-compose API стартует раньше, чем Postgres успевает принять соединения.</summary>
    private static async Task WaitForDatabaseAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        const int attempts = 10;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                if (await db.Database.CanConnectAsync(ct))
                {
                    return;
                }
            }
            catch (Exception exception) when (attempt < attempts)
            {
                logger.LogWarning("База ещё недоступна ({Attempt}/{Attempts}): {Message}", attempt, attempts, exception.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }
}
