using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Caching;
using RepairFlow.Api.Common;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Data;
using RepairFlow.Api.OpenApi;
using RepairFlow.Api.Realtime;
using RepairFlow.Api.Serialization;
using RepairFlow.Api.Services;

namespace RepairFlow.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<DemoOptions>(configuration.GetSection(DemoOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddAppPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Принимаем и формат ключей Npgsql, и URI вида postgresql://… — второй отдают облачные базы.
        var connectionString = PostgresConnectionString.Normalize(configuration.GetConnectionString("Default"));

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__migrations"));
        });

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrderAccessGuard, OrderAccessGuard>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderItemService, OrderItemService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        return services;
    }

    /// <summary>
    /// Кеш и живые обновления. Оба канала сериализуют данные MessagePack'ом — настройка одна на всё приложение.
    /// </summary>
    public static IServiceCollection AddAppMessagePackInfrastructure(this IServiceCollection services)
    {
        // Распределённый кеш в памяти процесса: для демо этого достаточно, а замена на Redis —
        // одна строка AddStackExchangeRedisCache, всё остальное останется как есть.
        services.AddDistributedMemoryCache();
        services.AddSingleton<ICacheStore, MessagePackCacheStore>();

        services
            .AddSignalR()
            .AddMessagePackProtocol(options => options.SerializerOptions = MessagePackConfig.Wire);

        services.AddSingleton<IOrderNotifier, OrderNotifier>();

        return services;
    }

    public static IServiceCollection AddAppAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt__Key не задан или короче 32 символов. Задайте секрет через переменные окружения.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    // Без этого «15 минут» на деле превращаются в 20: по умолчанию допуск 5 минут.
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // WebSocket не умеет слать заголовок Authorization, поэтому SignalR
                        // передаёт токен query-параметром. Принимаем его только на адресе хаба.
                        var token = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(token) &&
                            context.HttpContext.Request.Path.StartsWithSegments(OrdersHub.Path))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddSingleton<IAuthorizationHandler, OrderAccessHandler>();
        services.AddAuthorization(options => options.AddAppPolicies());

        return services;
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options => options.AddPolicy(AppPolicies.CorsPolicy, policy =>
        {
            if (origins.Length == 0)
            {
                policy.SetIsOriginAllowed(_ => true);
            }
            else
            {
                policy.WithOrigins(origins);
            }

            policy.AllowAnyHeader()
                .AllowAnyMethod()
                // Refresh-токен ездит в куке, значит запросы должны идти с credentials.
                .AllowCredentials();
        }));

        return services;
    }

    /// <summary>
    /// Документация на встроенном генераторе .NET 10: схему собирает Microsoft.AspNetCore.OpenApi,
    /// XML-комментарии подхватываются автоматически, UI рисует Scalar.
    /// </summary>
    public static IServiceCollection AddAppOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }
}
