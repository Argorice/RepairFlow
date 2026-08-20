namespace RepairFlow.Api.Common;

/// <summary>
/// Хелперы дат. Npgsql пишет в timestamptz только DateTime с Kind = Utc,
/// поэтому всё, что приходит снаружи, приводится к UTC в одном месте.
/// </summary>
public static class DateRange
{
    public static DateTime? ToUtc(DateTime? value) => value is null ? null : ToUtc(value.Value);

    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>Верхняя граница периода включительно: конец суток указанной даты.</summary>
    public static DateTime? EndOfDay(DateTime? value) =>
        value is null ? null : ToUtc(value.Value).Date.AddDays(1).AddTicks(-1);
}
