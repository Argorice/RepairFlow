using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Data;

/// <summary>
/// Ограничение выборки заявок по роли. Важно, что это часть IQueryable: условие уезжает в SQL
/// и лишние строки не покидают базу — фильтровать уже загруженный список было бы и медленно, и небезопасно.
/// </summary>
public static class OrderQueryScope
{
    public static IQueryable<Order> VisibleTo(this IQueryable<Order> query, Guid userId, UserRole role) => role switch
    {
        UserRole.Manager => query,
        UserRole.Technician => query.Where(o => o.AssignedTechnicianId == userId || o.AssignedTechnicianId == null),
        _ => query.Where(o => o.ClientId == userId)
    };
}
