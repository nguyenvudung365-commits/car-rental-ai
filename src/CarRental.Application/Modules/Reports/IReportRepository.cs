namespace CarRental.Application.Modules.Reports;

public interface IReportRepository
{
    /// <summary>from/to là ngày (bao gồm cả hai đầu).</summary>
    Task<RevenueReportDto> GetRevenueAsync(DateTime from, DateTime to);

    Task<OccupancyReportDto> GetOccupancyAsync(DateTime from, DateTime to);
}
