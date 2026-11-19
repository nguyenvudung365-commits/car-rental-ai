using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Reports;

public record RevenueByMonthDto(string Month, decimal Revenue, int Bookings);

public record RevenueByMethodDto(PaymentMethod Method, decimal Revenue, int Payments);

/// <summary>Doanh thu = tổng Payment đã thu; không gồm tiền cọc bảo đảm (nằm ở DepositTransactions).</summary>
public record RevenueReportDto(decimal TotalRevenue, int TotalBookings, List<RevenueByMonthDto> ByMonth, List<RevenueByMethodDto>? ByMethod = null);

/// <summary>OccupancyRate trong khoảng 0..1.</summary>
public record CarOccupancyDto(int CarId, string LicensePlate, double OccupancyRate);

public record OccupancyReportDto(int TotalCars, double AvgOccupancyRate, List<CarOccupancyDto> ByCar);
