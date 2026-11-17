using CarRental.Application.Common;

namespace CarRental.Application.Modules.Reports;

public class ReportService
{
    private const int MaxRangeDays = 366;
    private readonly IReportRepository _repository;

    public ReportService(IReportRepository repository) => _repository = repository;

    public Task<RevenueReportDto> GetRevenueAsync(DateTime? from, DateTime? to)
    {
        var (f, t) = NormalizeRange(from, to);
        return _repository.GetRevenueAsync(f, t);
    }

    public Task<OccupancyReportDto> GetOccupancyAsync(DateTime? from, DateTime? to)
    {
        var (f, t) = NormalizeRange(from, to);
        return _repository.GetOccupancyAsync(f, t);
    }

    /// <summary>Mặc định 30 ngày gần nhất; từ chối khoảng ngược hoặc quá dài.</summary>
    public static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
    {
        var t = (to ?? DateTime.UtcNow).Date;
        var f = (from ?? t.AddDays(-30)).Date;
        if (f > t) throw new ValidationAppException("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");
        if ((t - f).TotalDays > MaxRangeDays) throw new ValidationAppException($"Khoảng báo cáo tối đa {MaxRangeDays} ngày.");
        return (f, t);
    }
}
