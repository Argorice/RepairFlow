namespace RepairFlow.Api.Configuration;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>Сколько секунд живёт сводка дашборда. Ноль отключает кеш.</summary>
    public int DashboardSeconds { get; set; } = 60;

    public bool Enabled => DashboardSeconds > 0;
}
