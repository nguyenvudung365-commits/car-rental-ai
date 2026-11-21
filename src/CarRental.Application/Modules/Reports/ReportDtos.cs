namespace CarRental.Application.Modules.Reports;

public record RevenueByMonthDto(string Month, decimal Revenue, int Bookings);

/// <summary>Doanh thu = tổng Payment đã thu (DaCoc, DaThanhToan) có PaidAt trong khoảng.</summary>
public record RevenueReportDto(decimal TotalRevenue, int TotalBookings, List<RevenueByMonthDto> ByMonth);

/// <summary>OccupancyRate trong khoảng 0..1.</summary>
public record CarOccupancyDto(int CarId, string LicensePlate, double OccupancyRate);

public record OccupancyReportDto(int TotalCars, double AvgOccupancyRate, List<CarOccupancyDto> ByCar);
