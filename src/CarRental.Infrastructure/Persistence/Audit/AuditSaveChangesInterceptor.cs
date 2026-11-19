using System.Text.Json;
using CarRental.Application.Common;
using CarRental.Domain.Modules.Audit;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Branches;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Domain.Modules.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarRental.Infrastructure.Persistence.Audit;

/// <summary>
/// Ghi nhật ký thay đổi của các entity trong whitelist ngay trong SaveChanges.
/// Entity mới (chưa có Id) được ghi ở lần lưu thứ hai sau khi đã có khóa.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    // Thêm entity mới của các đợt sau vào đây.
    private static readonly HashSet<Type> Whitelist =
        [typeof(Car), typeof(Booking), typeof(Payment), typeof(ReturnRecord), typeof(Customer), typeof(BlacklistEntry),
         typeof(CustomerDocument), typeof(LoyaltyTransaction), typeof(HandoverRecord), typeof(DepositTransaction), typeof(BookingExtension),
         typeof(Coupon), typeof(Surcharge), typeof(Branch), typeof(CarMaintenance)];

    private const int MaxChangesLength = 8000;

    private readonly ICurrentUser _currentUser;
    private readonly List<(EntityEntry Entry, AuditLog Log)> _pendingAdded = new();
    private bool _saving;

    public AuditSaveChangesInterceptor(ICurrentUser currentUser) => _currentUser = currentUser;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Collect(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Collect(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var logs = TakeAddedLogs();
        if (logs.Count > 0 && eventData.Context is { } ctx)
        {
            _saving = true;
            try { ctx.AddRange(logs); ctx.SaveChanges(); }
            finally { _saving = false; }
        }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var logs = TakeAddedLogs();
        if (logs.Count > 0 && eventData.Context is { } ctx)
        {
            _saving = true;
            try { ctx.AddRange(logs); await ctx.SaveChangesAsync(cancellationToken); }
            finally { _saving = false; }
        }
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pendingAdded.Clear();

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendingAdded.Clear();
        return Task.CompletedTask;
    }

    private void Collect(DbContext? context)
    {
        if (context is null || _saving) return;
        _pendingAdded.Clear();

        context.ChangeTracker.DetectChanges();
        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (!Whitelist.Contains(entry.Entity.GetType())) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var log = BuildLog(entry);
            if (log is null) continue;

            if (entry.State == EntityState.Added)
                _pendingAdded.Add((entry, log)); // Id chỉ có sau khi lưu
            else
                context.Add(log); // lưu cùng lần SaveChanges này
        }
    }

    private List<AuditLog> TakeAddedLogs()
    {
        if (_saving) return [];
        var logs = _pendingAdded.Select(p =>
        {
            p.Log.EntityId = p.Entry.Property("Id").CurrentValue?.ToString();
            return p.Log;
        }).ToList();
        _pendingAdded.Clear();
        return logs;
    }

    private AuditLog? BuildLog(EntityEntry entry)
    {
        var entity = entry.Entity.GetType().Name;
        var changes = new Dictionary<string, object?[]>();
        var statusChanged = false;

        foreach (var p in entry.Properties)
        {
            var name = p.Metadata.Name;
            if (name is "UpdatedAt" or "Id" || p.IsTemporary) continue;

            object? oldValue = null, newValue = null;
            switch (entry.State)
            {
                case EntityState.Added:
                    if (p.CurrentValue is null) continue;
                    newValue = p.CurrentValue;
                    break;
                case EntityState.Deleted:
                    oldValue = p.OriginalValue;
                    break;
                default:
                    if (!p.IsModified || Equals(p.OriginalValue, p.CurrentValue)) continue;
                    oldValue = p.OriginalValue;
                    newValue = p.CurrentValue;
                    break;
            }

            if (name == "PasswordHash") { oldValue = oldValue is null ? null : "***"; newValue = newValue is null ? null : "***"; }
            if (name == "Status" && entry.State == EntityState.Modified) statusChanged = true;
            changes[name] = [oldValue, newValue];
        }

        if (entry.State == EntityState.Modified && changes.Count == 0) return null;

        var verb = entry.State switch { EntityState.Added => "Created", EntityState.Deleted => "Deleted", _ => "Updated" };
        var json = JsonSerializer.Serialize(changes);
        return new AuditLog
        {
            ActorId = _currentUser.UserId,
            ActorRole = _currentUser.Role,
            Action = $"{entity}.{verb}" + (statusChanged ? ".Status" : ""),
            EntityType = entity,
            EntityId = entry.State == EntityState.Added ? null : entry.Property("Id").CurrentValue?.ToString(),
            Changes = json.Length > MaxChangesLength ? json[..MaxChangesLength] : json,
            IpAddress = _currentUser.IpAddress
        };
    }
}
