using CarRental.Application.Common;
using CarRental.Application.Modules.Cars;
using ClosedXML.Excel;

namespace CarRental.Infrastructure.Reports;

/// <summary>Đọc/tạo xlsx nhập xe bằng ClosedXML. Dòng 1 là tiêu đề; dòng trống bị bỏ qua.</summary>
public class CarSheetReader : ICarSheetReader
{
    public List<CarSheetRow> Read(Stream xlsx, int maxRows)
    {
        try
        {
            using var wb = new XLWorkbook(xlsx);
            var ws = wb.Worksheets.First();
            var last = ws.LastRowUsed()?.RowNumber() ?? 0;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            // Kiểm trước khi đọc để file nhiều dòng không tốn bộ nhớ.
            if (last - 1 > maxRows)
                throw new BadRequestException("IMPORT_TOO_MANY_ROWS", $"Tệp tối đa {maxRows} dòng dữ liệu.");

            var headers = new Dictionary<int, string>();
            for (var c = 1; c <= lastCol; c++)
            {
                var h = ws.Cell(1, c).GetString().Trim();
                if (h.Length > 0) headers[c] = h;
            }

            var rows = new List<CarSheetRow>();
            for (var r = 2; r <= last; r++)
            {
                var cells = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (c, h) in headers)
                {
                    var cell = ws.Cell(r, c);
                    // Công thức không được đánh giá: lấy chữ hiển thị/giá trị đã lưu.
                    cells[h] = cell.IsEmpty() || cell.HasFormula ? null : cell.Value.IsNumber
                        ? cell.Value.GetNumber().ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : cell.GetString();
                }
                if (cells.Values.All(string.IsNullOrWhiteSpace)) continue;
                rows.Add(new CarSheetRow(r, cells));
            }
            return rows;
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            throw new BadRequestException("IMPORT_INVALID_FILE", "Không đọc được tệp Excel (.xlsx).");
        }
    }

    public byte[] BuildTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Xe");
        var example = new[] { "Sedan", "Toyota", "Vios", "51A-123.45", "5", "2", "800000", "Xe gia đình", "SoTuDong", "Xang", "2022", "0", "", "" };
        for (var c = 0; c < CarImportService.Columns.Length; c++)
        {
            ws.Cell(1, c + 1).SetValue(CarImportService.Columns[c]);
            ws.Cell(2, c + 1).SetValue(example[c]);
        }
        ws.Row(1).Style.Font.Bold = true;
        var guide = wb.Worksheets.Add("Huong dan");
        string[] lines =
        [
            "CarType: Sedan, SUV, Hatchback, MPV, Pickup, Luxury (hoặc số 1-6)",
            "Transmission: SoSan, SoTuDong (hoặc 1-2). FuelType: Xang, Dau, Dien, Hybrid (hoặc 1-4)",
            "BranchId: mã hoặc tên chi nhánh (để trống nếu chưa gán)",
            "SecurityDeposit, PricePerHour: chỉ Admin được nhập. Staff nhập xe sẽ ở trạng thái chờ duyệt.",
            "Tối đa 500 dòng, tệp tối đa 2 MB. Dòng 2 là ví dụ, hãy xóa hoặc thay trước khi nhập."
        ];
        for (var i = 0; i < lines.Length; i++) guide.Cell(i + 1, 1).SetValue(lines[i]);
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
