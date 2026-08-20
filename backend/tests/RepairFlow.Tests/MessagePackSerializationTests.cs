using MessagePack;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Domain.Enums;
using RepairFlow.Api.Serialization;
using Xunit;

namespace RepairFlow.Tests;

/// <summary>
/// MessagePack используется в трёх местах сразу — ответы API, SignalR и кеш, — и везде через одни
/// и те же настройки. Эти тесты фиксируют контракт: что уезжает и в каком виде.
/// </summary>
public class MessagePackSerializationTests
{
    [Fact]
    public void Order_survives_a_round_trip()
    {
        var order = SampleOrder();

        var bytes = MessagePackSerializer.Serialize(order, MessagePackConfig.Wire);
        var restored = MessagePackSerializer.Deserialize<OrderListItemDto>(bytes, MessagePackConfig.Wire);

        Assert.Equal(order.Id, restored.Id);
        Assert.Equal(order.Number, restored.Number);
        Assert.Equal(order.Status, restored.Status);
        Assert.Equal(order.Priority, restored.Priority);
        Assert.Equal(order.EstimatedCost, restored.EstimatedCost);
        Assert.Equal(order.CreatedAt, restored.CreatedAt);
        Assert.Equal(order.Client.FullName, restored.Client.FullName);
        Assert.Equal(order.Technician?.Id, restored.Technician?.Id);
    }

    [Fact]
    public void Enums_travel_as_strings_just_like_in_json()
    {
        var bytes = MessagePackSerializer.Serialize(SampleOrder(), MessagePackConfig.Wire);

        var asJson = MessagePackSerializer.ConvertToJson(bytes, MessagePackConfig.Wire);

        Assert.Contains("InProgress", asJson);
        Assert.DoesNotContain("\"Status\":3", asJson);
    }

    [Fact]
    public void Null_technician_stays_null()
    {
        var order = SampleOrder() with { Technician = null };

        var bytes = MessagePackSerializer.Serialize(order, MessagePackConfig.Wire);
        var restored = MessagePackSerializer.Deserialize<OrderListItemDto>(bytes, MessagePackConfig.Wire);

        Assert.Null(restored.Technician);
    }

    [Fact]
    public void Date_only_round_trips_through_the_custom_formatter()
    {
        var summary = SampleSummary();

        var bytes = MessagePackSerializer.Serialize(summary, MessagePackConfig.Wire);
        var restored = MessagePackSerializer.Deserialize<DashboardSummaryDto>(bytes, MessagePackConfig.Wire);

        Assert.Equal(summary.Daily.Count, restored.Daily.Count);
        Assert.Equal(summary.Daily[0].Date, restored.Daily[0].Date);
        Assert.Equal(summary.Daily[0].Created, restored.Daily[0].Created);
    }

    [Fact]
    public void Date_only_is_written_as_an_iso_string()
    {
        var bytes = MessagePackSerializer.Serialize(SampleSummary(), MessagePackConfig.Wire);

        Assert.Contains("2026-08-19", MessagePackSerializer.ConvertToJson(bytes, MessagePackConfig.Wire));
    }

    [Fact]
    public void Cache_options_compress_but_keep_the_same_data()
    {
        var summary = SampleSummary();

        var compressed = MessagePackSerializer.Serialize(summary, MessagePackConfig.Cache);
        var restored = MessagePackSerializer.Deserialize<DashboardSummaryDto>(compressed, MessagePackConfig.Cache);

        Assert.Equal(summary.TotalOrders, restored.TotalOrders);
        Assert.Equal(summary.RevenueInPeriod, restored.RevenueInPeriod);
        Assert.Equal(summary.ByStatus.Count, restored.ByStatus.Count);
        Assert.Equal(summary.TechnicianLoad[0].FullName, restored.TechnicianLoad[0].FullName);
    }

    [Fact]
    public void Realtime_event_round_trips()
    {
        var payload = new OrderEventDto(
            Guid.NewGuid(),
            "RF-2026-0007",
            OrderEventKind.StatusChanged,
            OrderStatus.ReadyForPickup,
            "Готова к выдаче",
            "Ремонт завершён",
            new DateTime(2026, 8, 19, 10, 0, 0, DateTimeKind.Utc));

        var bytes = MessagePackSerializer.Serialize(payload, MessagePackConfig.Wire);
        var restored = MessagePackSerializer.Deserialize<OrderEventDto>(bytes, MessagePackConfig.Wire);

        Assert.Equal(payload.OrderId, restored.OrderId);
        Assert.Equal(payload.Kind, restored.Kind);
        Assert.Equal(payload.Status, restored.Status);
        Assert.Equal(payload.Message, restored.Message);
    }

    private static OrderListItemDto SampleOrder() => new(
        Guid.Parse("44444444-4444-4444-4444-444444444444"),
        "RF-2026-0007",
        "Ноутбук",
        "Lenovo",
        "ThinkPad T14",
        OrderStatus.InProgress,
        "В работе",
        OrderPriority.High,
        new UserSummaryDto(Guid.NewGuid(), "Дмитрий Орлов", "client@demo.io", UserRole.Client),
        new UserSummaryDto(Guid.NewGuid(), "Павел Кузнецов", "master@demo.io", UserRole.Technician),
        18400.50m,
        null,
        new DateTime(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc),
        new DateTime(2026, 8, 12, 14, 0, 0, DateTimeKind.Utc),
        null);

    private static DashboardSummaryDto SampleSummary() => new(
        new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc),
        24,
        11,
        2,
        7,
        152300m,
        41.5,
        new[] { new StatusCountDto(OrderStatus.InProgress, "В работе", 4) },
        new[] { new DailyCountDto(new DateOnly(2026, 8, 19), 3, 1) },
        new[] { new TechnicianLoadDto(Guid.NewGuid(), "Павел Кузнецов", 4, 3, 61200m) });
}
