using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Mapping;
using RepairFlow.Api.Realtime;

namespace RepairFlow.Api.Services;

public interface ICommentService
{
    Task<IReadOnlyList<CommentDto>> GetAsync(Guid orderId, CancellationToken ct = default);

    Task<CommentDto> AddAsync(Guid orderId, CreateCommentRequest request, CancellationToken ct = default);
}

public sealed class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly IOrderAccessGuard _guard;
    private readonly ICurrentUser _currentUser;
    private readonly IOrderNotifier _notifier;

    public CommentService(
        AppDbContext db,
        IOrderAccessGuard guard,
        ICurrentUser currentUser,
        IOrderNotifier notifier)
    {
        _db = db;
        _guard = guard;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<IReadOnlyList<CommentDto>> GetAsync(Guid orderId, CancellationToken ct = default)
    {
        await _guard.LoadOrderAsync(orderId, OrderAccessRequirement.Read, ct);

        // Внутренние заметки отсекаются в самом запросе: клиенту они не уезжают даже в трафике.
        var isClient = _currentUser.IsClient;

        var comments = await _db.Comments.AsNoTracking()
            .Include(c => c.Author)
            .Where(c => c.OrderId == orderId && (!isClient || !c.IsInternal))
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        return comments.Select(c => c.ToDto()).ToList();
    }

    public async Task<CommentDto> AddAsync(Guid orderId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var order = await _guard.LoadOrderAsync(orderId, OrderAccessRequirement.Write, ct);

        var author = await _db.Users.FirstAsync(u => u.Id == _currentUser.Id, ct);

        var comment = new Comment
        {
            OrderId = orderId,
            AuthorId = author.Id,
            Author = author,
            Text = request.Text.Trim(),
            // Клиент физически не может создать внутреннюю заметку, что бы он ни прислал в теле запроса.
            IsInternal = !_currentUser.IsClient && request.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);

        var tracked = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (tracked is not null)
        {
            tracked.UpdatedAt = comment.CreatedAt;
        }

        await _db.SaveChangesAsync(ct);

        // Внутренняя заметка уедет только сотрудникам — разделение групп сделано в OrderNotifier.
        await _notifier.CommentAddedAsync(order, comment, ct);

        return comment.ToDto();
    }
}
