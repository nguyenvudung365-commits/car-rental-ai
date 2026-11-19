using CarRental.Application.Modules.Bookings;
using CarRental.Application.Common;

namespace CarRental.Application.Modules.Reports;

/// <summary>Xuất báo cáo ra tệp. Ô văn bản bắt đầu bằng = + - @ phải được vô hiệu (chống CSV/Excel injection).</summary>
public interface IReportExporter
{
    byte[] RevenueXlsx(RevenueReportDto report);
    byte[] RevenuePdf(RevenueReportDto report, DateTime from, DateTime to);
    byte[] OccupancyXlsx(OccupancyReportDto report);
    byte[] OccupancyPdf(OccupancyReportDto report, DateTime from, DateTime to);
    byte[] BookingsXlsx(IEnumerable<BookingDto> bookings);
}

public record ExportFile(byte[] Content, string ContentType, string FileName);

public class ReportExportService
{
    public const int MaxBookingRows = 5000;
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string Pdf = "application/pdf";

    private readonly ReportService _reports;
    private readonly IBookingService _bookings;
    private readonly IReportExporter _exporter;

    public ReportExportService(ReportService reports, IBookingService bookings, IReportExporter exporter)
    {
        _reports = reports;
        _bookings = bookings;
        _exporter = exporter;
    }

    public async Task<ExportFile> RevenueAsync(string? format, DateTime? from, DateTime? to)
    {
        var pdf = IsPdf(format);
        var (f, t) = ReportService.NormalizeRange(from, to);
        var data = await _reports.GetRevenueAsync(f, t);
        return new(pdf ? _exporter.RevenuePdf(data, f, t) : _exporter.RevenueXlsx(data), pdf ? Pdf : Xlsx, Name("bao-cao-doanh-thu", f, t, pdf));
    }

    public async Task<ExportFile> OccupancyAsync(string? format, DateTime? from, DateTime? to)
    {
        var pdf = IsPdf(format);
        var (f, t) = ReportService.NormalizeRange(from, to);
        var data = await _reports.GetOccupancyAsync(f, t);
        return new(pdf ? _exporter.OccupancyPdf(data, f, t) : _exporter.OccupancyXlsx(data), pdf ? Pdf : Xlsx, Name("bao-cao-lap-day", f, t, pdf));
    }

    public async Task<ExportFile> BookingsAsync(BookingFilterRequest filter)
    {
        var rows = new List<BookingDto>();
        for (var page = 1; rows.Count < MaxBookingRows; page++)
        {
            var (items, total) = await _bookings.GetFilteredAsync(filter with { Page = page, PageSize = 200 });
            rows.AddRange(items);
            if (items.Count == 0 || rows.Count >= total) break;
        }
        if (rows.Count > MaxBookingRows) rows.RemoveRange(MaxBookingRows, rows.Count - MaxBookingRows);
        return new(_exporter.BookingsXlsx(rows), Xlsx, $"don-thue-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    private static bool IsPdf(string? format) => (format ?? "xlsx").ToLowerInvariant() switch
    {
        "xlsx" => false,
        "pdf" => true,
        _ => throw new BadRequestException("INVALID_FORMAT", "format chỉ nhận xlsx hoặc pdf.")
    };

    private static string Name(string prefix, DateTime f, DateTime t, bool pdf)
        => $"{prefix}-{f:yyyyMMdd}-{t:yyyyMMdd}.{(pdf ? "pdf" : "xlsx")}";
}
