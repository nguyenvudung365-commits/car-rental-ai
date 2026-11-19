using CarRental.Application.Common;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Cars;

/// <summary>Kind: "Booking" hoặc "Maintenance". Status: tên enum (BookingStatus hoặc MaintenanceStatus).</summary>
public record CalendarItemDto(string Kind, int Id, DateTime Start, DateTime End, string Label, string Status);

public record CarCalendarDto(int CarId, string Name, string LicensePlate, CarStatus Status, List<CalendarItemDto> Items);

/// <summary>Truy vấn chỉ đọc (cài ở Infrastructure).</summary>
public interface ICalendarQuery
{
    Task<List<CarCalendarDto>> GetAsync(DateTime from, DateTime to, int? branchId);
}

public class CalendarService
{
    public const int MaxDays = 62;
    private readonly ICalendarQuery _query;
    public CalendarService(ICalendarQuery query) => _query = query;

    public Task<List<CarCalendarDto>> GetAsync(DateTime from, DateTime to, int? branchId)
    {
        if (to <= from) throw new ValidationAppException("Ngày kết thúc phải sau ngày bắt đầu.");
        if ((to - from).TotalDays > MaxDays) throw new ValidationAppException($"Lịch xe tối đa {MaxDays} ngày.");
        return _query.GetAsync(from, to, branchId);
    }
}
