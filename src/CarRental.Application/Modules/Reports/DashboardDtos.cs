using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Reports;

public record CarStatusCountDto(CarStatus Status, int Count);

/// <summary>EndDate là hạn trả; với đơn theo ngày hạn thực tế là hết ngày EndDate (xem ADR-011).</summary>
public record DashboardBookingDto(int BookingId, string CarName, string LicensePlate, string CustomerName, DateTime EndDate, RentalMode Mode);

/// <summary>TodayRevenue chỉ có với Admin (null với Staff).</summary>
public record DashboardDto(
    List<CarStatusCountDto> CarsByStatus,
    int RentingCount,
    List<DashboardBookingDto> DueTodayOrTomorrow,
    List<DashboardBookingDto> Overdue,
    int PendingConfirm,
    int PendingDocuments,
    int MaintenanceDue,
    decimal? TodayRevenue);

/// <summary>Truy vấn chỉ đọc (cài ở Infrastructure). now truyền vào để test được quanh mốc giờ.</summary>
public interface IDashboardQuery
{
    Task<DashboardDto> GetAsync(DateTime nowUtc, bool includeRevenue);
}
