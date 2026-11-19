using System.Globalization;
using CarRental.Application.Modules.Bookings;
using CarRental.Application.Modules.Reports;
using CarRental.Infrastructure.Pdf;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace CarRental.Infrastructure.Reports;

/// <summary>Xuất xlsx bằng ClosedXML (MIT) và PDF bằng PDFsharp.</summary>
public class ReportExporter : IReportExporter
{
    private static readonly CultureInfo Vi = new("vi-VN");

    public byte[] RevenueXlsx(RevenueReportDto r)
    {
        using var wb = new XLWorkbook();
        Sheet(wb, "Theo tháng", ["Tháng", "Doanh thu (VNĐ)", "Số đơn"], r.ByMonth.Select(m => new object?[] { m.Month, m.Revenue, m.Bookings }));
        Sheet(wb, "Theo phương thức", ["Phương thức", "Doanh thu (VNĐ)", "Số khoản thu"],
            (r.ByMethod ?? []).Select(m => new object?[] { m.Method.ToString(), m.Revenue, m.Payments }));
        return Save(wb);
    }

    public byte[] OccupancyXlsx(OccupancyReportDto r)
    {
        using var wb = new XLWorkbook();
        Sheet(wb, "Lấp đầy", ["Biển số", "Tỷ lệ lấp đầy"], r.ByCar.Select(c => new object?[] { c.LicensePlate, c.OccupancyRate }), percentCol: 2);
        return Save(wb);
    }

    public byte[] BookingsXlsx(IEnumerable<BookingDto> bookings)
        => SaveSingle("Đơn thuê", ["Mã", "Khách", "Xe", "Biển số", "Bắt đầu", "Kết thúc", "Số ngày", "Tổng tiền (VNĐ)", "Trạng thái", "Ghi chú"],
            bookings.Select(b => new object?[] { b.Id, b.CustomerName, b.CarName, b.LicensePlate, b.StartDate, b.EndDate, b.RentalDays, b.TotalPrice, b.Status, b.Note }));

    public byte[] RevenuePdf(RevenueReportDto r, DateTime from, DateTime to)
    {
        var lines = new List<string[]> { new[] { "Tháng", "Doanh thu", "Số đơn" } };
        lines.AddRange(r.ByMonth.Select(m => new[] { m.Month, Money(m.Revenue), m.Bookings.ToString(Vi) }));
        lines.Add(["", "", ""]);
        lines.Add(["Phương thức", "Doanh thu", "Số khoản thu"]);
        lines.AddRange((r.ByMethod ?? []).Select(m => new[] { m.Method.ToString(), Money(m.Revenue), m.Payments.ToString(Vi) }));
        return Pdf("BÁO CÁO DOANH THU", $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}   |   Tổng: {Money(r.TotalRevenue)}   |   {r.TotalBookings} đơn", lines);
    }

    public byte[] OccupancyPdf(OccupancyReportDto r, DateTime from, DateTime to)
    {
        var lines = new List<string[]> { new[] { "Biển số", "Tỷ lệ lấp đầy" } };
        lines.AddRange(r.ByCar.Select(c => new[] { c.LicensePlate, c.OccupancyRate.ToString("P1", Vi) }));
        return Pdf("BÁO CÁO TỶ LỆ LẤP ĐẦY", $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}   |   {r.TotalCars} xe   |   Trung bình {r.AvgOccupancyRate:P1}", lines);
    }

    /// <summary>Vô hiệu công thức: ô chữ bắt đầu bằng = + - @ (hoặc tab/CR) được thêm dấu nháy đơn phía trước.</summary>
    public static string Safe(string? s)
        => string.IsNullOrEmpty(s) ? "" : "=+-@\t\r".Contains(s[0]) ? "'" + s : s;

    private static string Money(decimal v) => v.ToString("N0", Vi) + " đ";

    private static byte[] SaveSingle(string name, string[] headers, IEnumerable<object?[]> rows)
    {
        using var wb = new XLWorkbook();
        Sheet(wb, name, headers, rows);
        return Save(wb);
    }

    private static void Sheet(XLWorkbook wb, string name, string[] headers, IEnumerable<object?[]> rows, int percentCol = 0)
    {
        var ws = wb.Worksheets.Add(name);
        for (var c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).SetValue(headers[c]);
        ws.Row(1).Style.Font.Bold = true;
        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
            {
                var cell = ws.Cell(r, c + 1);
                switch (row[c])
                {
                    case null: break;
                    case string s: cell.SetValue(Safe(s)); break;
                    case DateTime d: cell.SetValue(d); cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm"; break;
                    case decimal m: cell.SetValue(m); cell.Style.NumberFormat.Format = "#,##0"; break;
                    case double p when c + 1 == percentCol: cell.SetValue(p); cell.Style.NumberFormat.Format = "0.0%"; break;
                    case double p: cell.SetValue(p); break;
                    case int i: cell.SetValue(i); break;
                    default: cell.SetValue(Safe(row[c]!.ToString())); break;
                }
            }
            r++;
        }
        ws.Columns().AdjustToContents();
    }

    private static byte[] Save(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] Pdf(string title, string subtitle, List<string[]> rows)
    {
        PdfFonts.Ensure();
        using var doc = new PdfDocument();
        doc.Info.Title = title;
        var opt = new XPdfFontOptions(PdfFontEncoding.Unicode);
        var head = new XFont(PdfFonts.Family, 14, XFontStyleEx.Bold, opt);
        var bold = new XFont(PdfFonts.Family, 10, XFontStyleEx.Bold, opt);
        var body = new XFont(PdfFonts.Family, 10, XFontStyleEx.Regular, opt);

        PdfPage? page = null;
        XGraphics? gfx = null;
        var y = 0.0;
        void NewPage()
        {
            gfx?.Dispose();
            page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            y = 50;
        }
        NewPage();
        gfx!.DrawString(title, head, XBrushes.Black, 50, y, XStringFormats.TopLeft); y += 24;
        gfx.DrawString(subtitle, body, XBrushes.Black, 50, y, XStringFormats.TopLeft); y += 26;

        var cols = rows.Max(r => r.Length);
        var colW = (page!.Width.Point - 100) / cols;
        foreach (var row in rows)
        {
            if (y > page.Height.Point - 60) NewPage();
            var isHead = row.Length > 0 && rows.IndexOf(row) is var i && (i == 0 || (i > 0 && rows[i - 1].All(string.IsNullOrEmpty)));
            for (var c = 0; c < row.Length; c++)
                gfx!.DrawString(row[c], isHead ? bold : body, XBrushes.Black, 50 + c * colW, y, XStringFormats.TopLeft);
            y += 16;
        }
        gfx!.Dispose();
        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }
}
