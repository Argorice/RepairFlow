using System.Globalization;

namespace RepairFlow.Api.Common;

/// <summary>
/// Приводит строку подключения к виду, понятному Npgsql.
/// Облачные базы (Neon, Supabase, Railway) выдают адрес в формате URI —
/// <c>postgresql://user:password@host/db</c>, — а Npgsql такой формат не понимает
/// и падает на разборе. Вместо того чтобы ловить это уже в логах прода,
/// переводим URI в набор ключей сами.
/// </summary>
public static class PostgresConnectionString
{
    private const int DefaultPort = 5432;

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "Не задана строка подключения ConnectionStrings__Default.");
        }

        var value = raw.Trim();

        var isUri = value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

        if (!isUri)
        {
            return value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                "Строка подключения похожа на URI, но разобрать её не удалось.");
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(credentials[0]);
        var password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty;
        var database = uri.AbsolutePath.Trim('/');
        var port = uri.Port > 0 ? uri.Port : DefaultPort;

        return string.Join(';', new[]
        {
            $"Host={uri.Host}",
            $"Port={port.ToString(CultureInfo.InvariantCulture)}",
            $"Database={database}",
            $"Username={Quote(user)}",
            $"Password={Quote(password)}",
            // Облачные провайдеры принимают только TLS-соединения.
            "SSL Mode=Require",
            "Maximum Pool Size=10",
            // Neon отдаёт соединения через PgBouncer в transaction-режиме.
            "No Reset On Close=true"
        });
    }

    /// <summary>Значение с «;» или «=» внутри нужно взять в кавычки, иначе строка развалится.</summary>
    private static string Quote(string value) =>
        value.Contains(';', StringComparison.Ordinal) || value.Contains('=', StringComparison.Ordinal)
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
}
