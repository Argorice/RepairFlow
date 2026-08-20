using RepairFlow.Api.Domain;
using RepairFlow.Api.Domain.Enums;
using Xunit;

namespace RepairFlow.Tests;

/// <summary>
/// Машина состояний — самая ценная часть бизнес-логики: именно она отличает продукт от CRUD.
/// Поэтому тестами закрыт и граф переходов, и права ролей на них.
/// </summary>
public class OrderStatusMachineTests
{
    [Fact]
    public void Technician_takes_new_order_into_diagnostics()
    {
        var result = OrderStatusMachine.Validate(UserRole.Technician, OrderStatus.New, OrderStatus.Diagnostics);

        Assert.True(result.IsAllowed);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Client_cannot_start_diagnostics()
    {
        var result = OrderStatusMachine.Validate(UserRole.Client, OrderStatus.New, OrderStatus.Diagnostics);

        Assert.False(result.IsAllowed);
        Assert.Contains("Клиент", result.Error);
    }

    [Fact]
    public void Client_can_cancel_new_order()
    {
        Assert.True(OrderStatusMachine.Validate(UserRole.Client, OrderStatus.New, OrderStatus.Cancelled).IsAllowed);
    }

    [Fact]
    public void Client_cannot_cancel_order_in_progress()
    {
        Assert.False(OrderStatusMachine.Validate(UserRole.Client, OrderStatus.InProgress, OrderStatus.Cancelled).IsAllowed);
    }

    [Fact]
    public void Client_approves_estimate()
    {
        Assert.True(OrderStatusMachine
            .Validate(UserRole.Client, OrderStatus.AwaitingEstimateApproval, OrderStatus.InProgress)
            .IsAllowed);
    }

    [Fact]
    public void Client_rejects_estimate()
    {
        Assert.True(OrderStatusMachine
            .Validate(UserRole.Client, OrderStatus.AwaitingEstimateApproval, OrderStatus.ClientRejected)
            .IsAllowed);
    }

    [Fact]
    public void Technician_cannot_approve_estimate_for_client()
    {
        Assert.False(OrderStatusMachine
            .Validate(UserRole.Technician, OrderStatus.AwaitingEstimateApproval, OrderStatus.InProgress)
            .IsAllowed);
    }

    [Fact]
    public void Transition_to_same_status_is_rejected()
    {
        var result = OrderStatusMachine.Validate(UserRole.Manager, OrderStatus.InProgress, OrderStatus.InProgress);

        Assert.False(result.IsAllowed);
        Assert.Contains("уже находится", result.Error);
    }

    [Fact]
    public void Undefined_transition_is_rejected()
    {
        var result = OrderStatusMachine.Validate(UserRole.Manager, OrderStatus.New, OrderStatus.Completed);

        Assert.False(result.IsAllowed);
        Assert.Contains("не предусмотрен", result.Error);
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.ClientRejected)]
    public void Terminal_statuses_have_no_outgoing_transitions(OrderStatus terminal)
    {
        Assert.True(OrderStatusMachine.IsTerminal(terminal));
        Assert.Empty(OrderStatusMachine.AllowedTargets(terminal));
        Assert.False(OrderStatusMachine.Validate(UserRole.Manager, terminal, OrderStatus.InProgress).IsAllowed);
    }

    [Fact]
    public void Manager_can_perform_every_transition_defined_in_the_graph()
    {
        foreach (var from in Enum.GetValues<OrderStatus>())
        {
            foreach (var to in OrderStatusMachine.AllowedTargets(from))
            {
                Assert.True(
                    OrderStatusMachine.Validate(UserRole.Manager, from, to).IsAllowed,
                    $"менеджер должен уметь {from} → {to}");
            }
        }
    }

    [Fact]
    public void Client_sees_only_approve_and_reject_while_estimate_is_pending()
    {
        var targets = OrderStatusMachine.AllowedTargetsFor(UserRole.Client, OrderStatus.AwaitingEstimateApproval);

        Assert.Equal(new[] { OrderStatus.InProgress, OrderStatus.ClientRejected }, targets);
    }

    [Fact]
    public void Technician_sees_only_diagnostics_for_a_new_order()
    {
        var targets = OrderStatusMachine.AllowedTargetsFor(UserRole.Technician, OrderStatus.New);

        Assert.Equal(OrderStatus.Diagnostics, Assert.Single(targets));
    }

    [Fact]
    public void Estimate_is_required_only_when_sending_it_for_approval()
    {
        Assert.True(OrderStatusMachine.RequiresNonEmptyEstimate(OrderStatus.AwaitingEstimateApproval));
        Assert.False(OrderStatusMachine.RequiresNonEmptyEstimate(OrderStatus.InProgress));
        Assert.False(OrderStatusMachine.RequiresNonEmptyEstimate(OrderStatus.Completed));
    }

    [Theory]
    [InlineData(OrderStatus.New, true)]
    [InlineData(OrderStatus.Diagnostics, true)]
    [InlineData(OrderStatus.InProgress, true)]
    [InlineData(OrderStatus.AwaitingEstimateApproval, false)]
    [InlineData(OrderStatus.ReadyForPickup, false)]
    [InlineData(OrderStatus.Completed, false)]
    public void Estimate_is_editable_only_before_and_after_approval_but_not_during(OrderStatus status, bool editable)
    {
        Assert.Equal(editable, OrderStatusMachine.IsEstimateEditable(status));
    }

    [Fact]
    public void Happy_path_walks_from_new_to_completed()
    {
        var path = new[]
        {
            (Role: UserRole.Technician, From: OrderStatus.New, To: OrderStatus.Diagnostics),
            (Role: UserRole.Technician, From: OrderStatus.Diagnostics, To: OrderStatus.AwaitingEstimateApproval),
            (Role: UserRole.Client, From: OrderStatus.AwaitingEstimateApproval, To: OrderStatus.InProgress),
            (Role: UserRole.Technician, From: OrderStatus.InProgress, To: OrderStatus.ReadyForPickup),
            (Role: UserRole.Technician, From: OrderStatus.ReadyForPickup, To: OrderStatus.Completed)
        };

        foreach (var step in path)
        {
            Assert.True(
                OrderStatusMachine.Validate(step.Role, step.From, step.To).IsAllowed,
                $"шаг {step.From} → {step.To} роли {step.Role} должен быть разрешён");
        }
    }

    [Fact]
    public void Estimate_can_be_recalculated_by_returning_to_diagnostics()
    {
        Assert.True(OrderStatusMachine
            .Validate(UserRole.Technician, OrderStatus.AwaitingEstimateApproval, OrderStatus.Diagnostics)
            .IsAllowed);
    }

    [Fact]
    public void Completion_is_recognised_only_for_completed_status()
    {
        Assert.True(OrderStatusMachine.IsCompletion(OrderStatus.Completed));
        Assert.False(OrderStatusMachine.IsCompletion(OrderStatus.ReadyForPickup));
        Assert.False(OrderStatusMachine.IsCompletion(OrderStatus.Cancelled));
    }
}
