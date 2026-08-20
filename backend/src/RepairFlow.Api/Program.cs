using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Http.Features;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Extensions;
using RepairFlow.Api.Filters;
using RepairFlow.Api.Middleware;
using RepairFlow.Api.Realtime;
using RepairFlow.Api.Serialization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppOptions(builder.Configuration)
    .AddAppPersistence(builder.Configuration)
    .AddAppServices()
    .AddAppMessagePackInfrastructure()
    .AddAppAuth(builder.Configuration)
    .AddAppCors(builder.Configuration)
    .AddAppOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        // JSON остаётся форматом по умолчанию, MessagePack включается заголовком Accept.
        MessagePackConfig.AddMessagePackFormatters(options);
    })
    .AddJsonOptions(options =>
    {
        // Enum'ы наружу уезжают строками: «InProgress» читается лучше, чем «3», и не ломается при вставке нового значения.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<FormOptions>(options =>
{
    var storage = builder.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
    options.MultipartBodyLengthLimit = storage.MaxFileSizeBytes + 1024 * 1024;
});

var app = builder.Build();

await app.InitializeDatabaseAsync();

// Обработчик исключений стоит первым: всё, что упадёт ниже, превратится в ProblemDetails.
app.UseAppExceptionHandling();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors(AppPolicies.CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrdersHub>(OrdersHub.Path);

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapGet("/", () => Results.Redirect("/scalar/v1"))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.Run();

/// <summary>Нужен, чтобы тестовый проект мог сослаться на сборку API.</summary>
public partial class Program
{
}
