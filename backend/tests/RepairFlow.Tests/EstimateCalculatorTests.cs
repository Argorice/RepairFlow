using RepairFlow.Api.Domain;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;
using Xunit;

namespace RepairFlow.Tests;

public class EstimateCalculatorTests
{
    [Fact]
    public void Line_total_multiplies_quantity_by_price()
    {
        Assert.Equal(3300m, EstimateCalculator.LineTotal(1.5m, 2200m));
    }

    [Fact]
    public void Line_total_rounds_half_away_from_zero()
    {
        // 3 × 33.335 = 100.005 — банковское округление дало бы 100.00, а в счёте клиента ждут 100.01.
        Assert.Equal(100.01m, EstimateCalculator.LineTotal(3m, 33.335m));
    }

    [Fact]
    public void Negative_quantity_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EstimateCalculator.LineTotal(-1m, 100m));
    }

    [Fact]
    public void Negative_price_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EstimateCalculator.LineTotal(1m, -100m));
    }

    [Fact]
    public void Empty_estimate_has_zero_totals()
    {
        var result = EstimateCalculator.Calculate(Array.Empty<OrderItem>());

        Assert.Equal(0m, result.Total);
        Assert.Equal(0m, result.PartsTotal);
        Assert.Equal(0m, result.LaborTotal);
        Assert.Equal(0, result.ItemCount);
    }

    [Fact]
    public void Parts_and_labor_are_counted_separately()
    {
        var items = new[]
        {
            Item(OrderItemType.Labor, "Диагностика", 1m, 1500m),
            Item(OrderItemType.Labor, "Замена дисплея", 2.5m, 2000m),
            Item(OrderItemType.Part, "Дисплейный модуль", 1m, 18900m),
            Item(OrderItemType.Part, "Клейкая лента", 2m, 250m)
        };

        var result = EstimateCalculator.Calculate(items);

        Assert.Equal(6500m, result.LaborTotal);
        Assert.Equal(19400m, result.PartsTotal);
        Assert.Equal(25900m, result.Total);
        Assert.Equal(4, result.ItemCount);
    }

    [Fact]
    public void Total_is_the_sum_of_rounded_lines()
    {
        var items = new[]
        {
            Item(OrderItemType.Labor, "Работа A", 3m, 33.335m),
            Item(OrderItemType.Labor, "Работа B", 3m, 33.335m)
        };

        // Каждая строка округляется отдельно: 100.01 + 100.01, а не round(200.01).
        Assert.Equal(200.02m, EstimateCalculator.Total(items));
    }

    [Fact]
    public void Fractional_labor_hours_are_supported()
    {
        var items = new[] { Item(OrderItemType.Labor, "Пайка", 0.25m, 2200m) };

        Assert.Equal(550m, EstimateCalculator.Total(items));
    }

    [Fact]
    public void Zero_price_items_are_allowed()
    {
        var items = new[] { Item(OrderItemType.Part, "Гарантийная замена", 1m, 0m) };

        var result = EstimateCalculator.Calculate(items);

        Assert.Equal(0m, result.Total);
        Assert.Equal(1, result.ItemCount);
    }

    private static OrderItem Item(OrderItemType type, string name, decimal quantity, decimal price) => new()
    {
        Type = type,
        Name = name,
        Quantity = quantity,
        UnitPrice = price
    };
}
