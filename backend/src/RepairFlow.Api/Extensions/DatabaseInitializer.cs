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
    /// Здесь не годятся два очевидных способа. EnsureCreated создаёт схему только заодно
    /// с созданием базы, а у облачного провайдера база уже есть — метод молча вернёт false.
    /// HasTables у EF считает таблицы во всех схемах, кроме системных: если провайдер положил
    /// в базу что-то своё (Neon, например, заводит схему neon_auth), проверка решит, что схема
    /// на месте, и наши таблицы так и не появятся. Наружу это вылезает первым запросом:
    /// relation "users" does not exist.
    ///
    /// Поэтому проверяем не «есть ли хоть какие-то таблицы», а есть ли конкретно наша.
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
        if (await HasOwnSchemaAsync(db, ct))
        {
            logger.LogInformation("Таблицы приложения уже существуют — создание пропущено.");
            return;
        }

        var creator = db.Database.GetService<IRelationalDatabaseCreator>();
        await creator.CreateTablesAsync(ct);

        logger.LogWarning("Миграции не найдены — схема создана напрямую из модели.");
    }

    /// <summary>Есть ли в базе именно наша таблица заявок и пользователей, а не чужие.</summary>
    private static async Task<bool> HasOwnSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        // SqlQuery ожидает, что скалярная колонка называется Value.
        var result = await db.Database
            .SqlQuery<bool>($"""SELECT (to_regclass('public.users') IS NOT NULL) AS "Value" """)
            .SingleAsync(ct);

        return result;
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
