using System.Globalization;

namespace RepairFlow.Api.Domain;

/// <summary>
/// Номер заявки вида RF-2026-0001: последовательный в рамках года.
/// Форматирование и разбор — чистые функции, а конкурентная выдача следующего номера
/// решается в сервисе через блокировку на уровне транзакции Postgres.
/// </summary>
public static class OrderNumberGenerator
{
    public const string Prefix = "RF";

    /// <summary>Минимальная ширина порядкового номера; при переполнении номер просто становится длиннее.</summary>
    public const int SequenceWidth = 4;

    public static string Format(int year, int sequence)
    {
        if (year < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Некорректный год.");
        }

        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Порядковый номер начинается с единицы.");
        }

        var yearPart = year.ToString("D4", CultureInfo.InvariantCulture);
        var sequencePart = sequence.ToString("D" + SequenceWidth.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        return Prefix + "-" + yearPart + "-" + sequencePart;
    }

    /// <summary>Префикс для выборки номеров нужного года: «RF-2026-».</summary>
    public static string YearPrefix(int year) =>
        Prefix + "-" + year.ToString("D4", CultureInfo.InvariantCulture) + "-";

    /// <summary>Разбирает порядковый номер из строки. Возвращает false, если формат чужой.</summary>
    public static bool TryParseSequence(string? number, out int year, out int sequence)
    {
        year = 0;
        sequence = 0;

        if (string.IsNullOrWhiteSpace(number))
        {
            return false;
        }

        var parts = number.Split('-');
        if (parts.Length != 3 || !string.Equals(parts[0], Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out year)
               && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out sequence)
               && year > 0
               && sequence > 0;
    }

    /// <summary>
    /// Следующий номер года по последнему выданному. Если за год номеров ещё не было
    /// (или последний номер в чужом формате) — начинает с единицы.
    /// </summary>
    public static string Next(int year, string? lastNumberOfYear)
    {
        var sequence = TryParseSequence(lastNumberOfYear, out var parsedYear, out var lastSequence) && parsedYear == year
            ? lastSequence + 1
            : 1;

        return Format(year, sequence);
    }
}
