namespace CarRental.Application.Modules.Audit;

public record AuditLogDto(
    long Id,
    int? ActorId,
    string? ActorName,
    string? ActorRole,
    string Action,
    string EntityType,
    string? EntityId,
    string? Changes,
    string? IpAddress,
    DateTime CreatedAt);

public record AuditLogFilterRequest(
    string? EntityType,
    string? EntityId,
    int? ActorId,
    string? Action,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20);
