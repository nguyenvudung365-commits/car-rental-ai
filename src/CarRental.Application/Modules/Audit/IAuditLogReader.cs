namespace CarRental.Application.Modules.Audit;

public interface IAuditLogReader
{
    Task<(List<AuditLogDto> Items, int TotalCount)> QueryAsync(AuditLogFilterRequest filter);
}
