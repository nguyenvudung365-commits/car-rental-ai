using System.Globalization;
using System.Reflection;
using CarRental.Application.Modules.Bookings;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace CarRental.Infrastructure.Pdf;

/// <summary>Sinh hợp đồng PDF bằng PDFsharp (MIT, thuần managed); nhúng font Be Vietnam Pro (OFL) để hiển thị tiếng Việt.</summary>
public class ContractPdfGenerator : IContractPdfGenerator
{
    private const string FontFamily = "BeVietnamPro";
    private static readonly object FontLock = new();
    private static bool _fontReady;
    private static readonly CultureInfo Vi = new("vi-VN");

    public byte[] Generate(ContractData d)
    {
        EnsureFontResolver();

        using var doc = new PdfDocument();
        doc.Info.Title = $"Hợp đồng thuê xe #{d.BookingId}";
        var page = doc.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        using var gfx = XGraphics.FromPdfPage(page);

        var opt = new XPdfFontOptions(PdfFontEncoding.Unicode);
        var title = new XFont(FontFamily, 16, XFontStyleEx.Bold, opt);
        var head = new XFont(FontFamily, 11, XFontStyleEx.Bold, opt);
        var body = new XFont(FontFamily, 10, XFontStyleEx.Regular, opt);

        const double left = 50;
        var width = page.Width.Point - 2 * left;
        var y = 50.0;

        void Center(string text, XFont f)
        {
            gfx.DrawString(text, f, XBrushes.Black, new XRect(left, y, width, 20), XStringFormats.TopCenter);
            y += 22;
        }

        void Line(string text, XFont f)
        {
            foreach (var l in Wrap(gfx, text, f, width))
            {
                gfx.DrawString(l, f, XBrushes.Black, left, y, XStringFormats.TopLeft);
                y += 16;
            }
        }

        Center("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", head);
        Center("Độc lập - Tự do - Hạnh phúc", body);
        y += 6;
        Center("HỢP ĐỒNG THUÊ XE", title);
        Center($"Số: HD-{d.BookingId:D6}   |   Ngày lập: {d.CreatedAt.ToLocalTime():dd/MM/yyyy}", body);
        y += 8;

        Line("BÊN CHO THUÊ (Bên A): Car Rental AI", head);
        y += 6;
        Line("BÊN THUÊ (Bên B)", head);
        Line($"Họ và tên: {d.CustomerName}", body);
        Line($"Email: {d.CustomerEmail}    Điện thoại: {d.CustomerPhone}", body);
        Line($"CCCD/CMND: {Or(d.CustomerIdentityNumber)}", body);
        Line($"Địa chỉ: {Or(d.CustomerAddress)}", body);
        y += 6;

        Line("THÔNG TIN XE", head);
        Line($"Xe: {d.CarName} ({d.Seats} chỗ)    Biển số: {d.LicensePlate}", body);
        y += 6;

        Line("THỜI GIAN VÀ GIÁ THUÊ", head);
        Line($"Từ ngày: {d.StartDate:dd/MM/yyyy HH:mm}    Đến ngày: {d.EndDate:dd/MM/yyyy HH:mm}    Số ngày: {d.RentalDays}", body);
        Line($"Giá chốt mỗi ngày: {Money(d.FinalPricePerDay)}", body);
        Line($"Tổng tiền thuê: {Money(d.TotalPrice)}", body);
        Line($"Trạng thái booking: {StatusText(d.BookingStatus)}", body);
        y += 6;

        Line("ĐIỀU KHOẢN", head);
        Line("1. Bên B nhận xe đúng thời gian, sử dụng đúng mục đích và hoàn trả xe nguyên trạng.", body);
        Line("2. Trả xe trễ so với ngày kết thúc: phí trễ = số ngày trễ x giá chốt mỗi ngày.", body);
        Line("3. Hư hỏng phát sinh do Bên B được ghi nhận khi trả xe và Bên B chịu chi phí tương ứng.", body);
        Line("4. Tổng thanh toán = tiền thuê + phí trễ + phí hư hỏng.", body);

        if (d.ReturnedAt.HasValue)
        {
            y += 6;
            Line("KẾT QUẢ TRẢ XE", head);
            Line($"Ngày trả: {d.ReturnedAt:dd/MM/yyyy HH:mm}    Số ngày trễ: {d.LateDays ?? 0}", body);
            Line($"Phí trễ: {Money(d.LateFee ?? 0)}    Phí hư hỏng: {Money(d.DamageFee ?? 0)}", body);
            Line($"Tổng thanh toán: {Money(d.FinalAmount ?? d.TotalPrice)}", body);
        }

        y = Math.Max(y + 30, 640);
        gfx.DrawString("ĐẠI DIỆN BÊN A", head, XBrushes.Black, new XRect(left, y, width / 2, 20), XStringFormats.TopCenter);
        gfx.DrawString("BÊN B", head, XBrushes.Black, new XRect(left + width / 2, y, width / 2, 20), XStringFormats.TopCenter);
        y += 18;
        gfx.DrawString("(Ký, ghi rõ họ tên)", body, XBrushes.Black, new XRect(left, y, width / 2, 20), XStringFormats.TopCenter);
        gfx.DrawString("(Ký, ghi rõ họ tên)", body, XBrushes.Black, new XRect(left + width / 2, y, width / 2, 20), XStringFormats.TopCenter);

        using var ms = new MemoryStream();
        doc.Save(ms, false);
        return ms.ToArray();
    }

    private static string StatusText(string s) => s switch
    {
        "DaXacNhan" => "Đã xác nhận",
        "DangThue" => "Đang thuê",
        "DaTraXe" => "Đã trả xe",
        _ => s
    };

    private static string Or(string? s) => string.IsNullOrWhiteSpace(s) ? "........................" : s;

    private static string Money(decimal v) => v.ToString("N0", Vi) + " VNĐ";

    private static IEnumerable<string> Wrap(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        var line = "";
        foreach (var word in text.Split(' '))
        {
            var candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && gfx.MeasureString(candidate, font).Width > maxWidth)
            {
                yield return line;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }
        yield return line;
    }

    private static void EnsureFontResolver()
    {
        if (_fontReady) return;
        lock (FontLock)
        {
            if (_fontReady) return;
            GlobalFontSettings.FontResolver ??= new EmbeddedFontResolver();
            _fontReady = true;
        }
    }

    private sealed class EmbeddedFontResolver : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
            => string.Equals(familyName, FontFamily, StringComparison.OrdinalIgnoreCase)
                ? new FontResolverInfo(isBold ? "BeVietnamPro-Bold" : "BeVietnamPro-Regular")
                : null;

        public byte[]? GetFont(string faceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream($"CarRental.Infrastructure.Pdf.Fonts.{faceName}.ttf");
            if (s == null) return null;
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
