using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
            await CreateSchemaWithoutMigrationsAsync(db, logger, ct);
        }

        if (demo.SeedOnStartup)
        {
            await DbSeeder.SeedAsync(db, demo, logger, ct);
        }
    }

    /// <summary>
    /// Запасной путь для клона, в котором миграции ещё не сгенерированы.
    ///
    /// Здесь нельзя использовать EnsureCreated: он создаёт схему только заодно с созданием базы,
    /// а у облачного провайдера база уже существует — метод молча возвращает false и не создаёт
    /// ни одной таблицы. Наружу это вылезает первым же запросом: relation "users" does not exist.
    /// Поэтому таблицы создаются напрямую, если их ещё нет.
    ///
    /// Когда миграции появятся, этот путь перестанет выполняться. Но базу, поднятую этим способом,
    /// миграции уже не примут — истории в __migrations нет. Перед первым «dotnet ef database update»
    /// схему нужно очистить (в Neon проще всего сбросить ветку).
    /// </summary>
    private static async Task CreateSchemaWithoutMigrationsAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var creator = db.Database.GetService<IRelationalDatabaseCreator>();

        if (await creator.HasTablesAsync(ct))
        {
            logger.LogInformation("Схема уже существует — создание пропущено.");
            return;
        }

        await creator.CreateTablesAsync(ct);
        logger.LogWarning("Миграции не найдены — схема создана напрямую из модели.");
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
