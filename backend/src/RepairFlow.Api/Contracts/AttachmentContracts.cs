namespace RepairFlow.Api.Contracts;

public sealed record AttachmentDto(
    Guid Id,
    Guid OrderId,
    string FileName,
    string ContentType,
    long SizeBytes,
    bool IsImage,
    UserSummaryDto UploadedBy,
    DateTime UploadedAt,
    string Url);
