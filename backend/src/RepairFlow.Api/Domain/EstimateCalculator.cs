using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Domain;

/// <summary>Разбивка сметы: отдельно запчасти, отдельно работы, итог.</summary>
public readonly record struct EstimateBreakdown(decimal PartsTotal, decimal LaborTotal, decimal Total, int ItemCount);

/// <summary>
/// Расчёт сметы. Вынесен из сервиса намеренно: деньги — это то место, где ошибка округления
/// стоит дороже всего, поэтому логика чистая и покрыта тестами.
/// </summary>
public static class EstimateCalculator
{
    /// <summary>Знаков после запятой в денежных суммах.</summary>
    public const int MoneyScale = 2;

    /// <summary>Сумма одной позиции с округлением до копеек.</summary>
    public static decimal LineTotal(decimal quantity, decimal unitPrice)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Количество не может быть отрицательным.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Цена не может быть отрицательной.");
        }

        return Round(quantity * unitPrice);
    }

    public static decimal LineTotal(OrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return LineTotal(item.Quantity, item.UnitPrice);
    }

    /// <summary>Итог по списку позиций. Округляется каждая строка, а не сумма — так же, как в счёте на бумаге.</summary>
    public static decimal Total(IEnumerable<OrderItem> items) => Calculate(items).Total;

    public static EstimateBreakdown Calculate(IEnumerable<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        decimal parts = 0m;
        decimal labor = 0m;
        var count = 0;

        foreach (var item in items)
        {
            var line = LineTotal(item);
            if (item.Type == OrderItemType.Part)
            {
                parts += line;
            }
            else
            {
                labor += line;
            }

            count++;
        }

        return new EstimateBreakdown(Round(parts), Round(labor), Round(parts + labor), count);
    }

    private static decimal Round(decimal value) => Math.Round(value, MoneyScale, MidpointRounding.AwayFromZero);
}
