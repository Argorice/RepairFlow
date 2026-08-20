using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Contracts;

public sealed record CommentDto(
    Guid Id,
    string Text,
    bool IsInternal,
    UserSummaryDto Author,
    DateTime CreatedAt);

/// <summary>IsInternal может выставить только мастер или менеджер — клиенту флаг игнорируется.</summary>
public sealed record CreateCommentRequest(string Text, bool IsInternal);
