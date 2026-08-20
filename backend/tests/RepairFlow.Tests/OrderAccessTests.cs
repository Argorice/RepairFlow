using RepairFlow.Api.Authorization;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;
using Xunit;

namespace RepairFlow.Tests;

/// <summary>Права на конкретную заявку — второе место, где ошибка стоит дорого: чужие данные видны быть не должны.</summary>
public class OrderAccessTests
{
    private static readonly Guid ClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TechnicianId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherTechnicianId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Client_sees_own_order()
    {
        Assert.True(OrderAccessHandler.IsGranted(
            UserRole.Client, ClientId, NewOrder(ClientId, TechnicianId), OrderAccessLevel.Read));
    }

    [Fact]
    public void Client_does_not_see_someone_elses_order()
    {
        Assert.False(OrderAccessHandler.IsGranted(
            UserRole.Client, ClientId, NewOrder(Guid.NewGuid(), TechnicianId), OrderAccessLevel.Read));
    }

    [Fact]
    public void Client_cannot_manage_even_own_order()
    {
        Assert.False(OrderAccessHandler.IsGranted(
            UserRole.Client, ClientId, NewOrder(ClientId, TechnicianId), OrderAccessLevel.Manage));
    }

    [Fact]
    public void Technician_works_with_assigned_order()
    {
        Assert.True(OrderAccessHandler.IsGranted(
            UserRole.Technician, TechnicianId, NewOrder(ClientId, TechnicianId), OrderAccessLevel.Write));
    }

    [Fact]
    public void Technician_can_pick_up_an_unassigned_order()
    {
        Assert.True(OrderAccessHandler.IsGranted(
            UserRole.Technician, TechnicianId, NewOrder(ClientId, null), OrderAccessLevel.Write));
    }

    [Fact]
    public void Technician_does_not_see_order_of_another_technician()
    {
        Assert.False(OrderAccessHandler.IsGranted(
            UserRole.Technician, TechnicianId, NewOrder(ClientId, OtherTechnicianId), OrderAccessLevel.Read));
    }

    [Fact]
    public void Technician_cannot_assign_orders()
    {
        Assert.False(OrderAccessHandler.IsGranted(
            UserRole.Technician, TechnicianId, NewOrder(ClientId, TechnicianId), OrderAccessLevel.Manage));
    }

    [Theory]
    [InlineData(OrderAccessLevel.Read)]
    [InlineData(OrderAccessLevel.Write)]
    [InlineData(OrderAccessLevel.Manage)]
    public void Manager_has_full_access(OrderAccessLevel level)
    {
        Assert.True(OrderAccessHandler.IsGranted(
            UserRole.Manager, Guid.NewGuid(), NewOrder(ClientId, OtherTechnicianId), level));
    }

    private static Order NewOrder(Guid clientId, Guid? assignedTechnicianId) => new()
    {
        Number = "RF-2026-0001",
        ClientId = clientId,
        AssignedTechnicianId = assignedTechnicianId,
        DeviceType = "Ноутбук",
        Brand = "Lenovo",
        Model = "ThinkPad",
        ProblemDescription = "Не включается",
        Status = OrderStatus.New
    };
}
