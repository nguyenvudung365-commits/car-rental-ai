using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using CarRental.Application.Common;
using CarRental.Application.Modules.Pricing;
using CarRental.Domain.Common;
using CarEntity = CarRental.Domain.Modules.Cars.Car;

namespace CarRental.Application.Modules.Cars;

public record ImportRowError(int Row, string Column, string Message);

/// <summary>Committed = true khi đã ghi vào DB (không dry run và không có lỗi).</summary>
public record ImportResultDto(int Total, int Valid, List<ImportRowError> Errors, bool Committed = false);

/// <summary>Một dòng dữ liệu thô của trang tính: Row là số dòng trong Excel, Cells theo tên cột (không phân biệt hoa thường).</summary>
public record CarSheetRow(int Row, IReadOnlyDictionary<string, string?> Cells);

/// <summary>Đọc/tạo tệp xlsx (cài ở Infrastructure bằng ClosedXML). Read ném ValidationAppException khi tệp hỏng hoặc quá maxRows.</summary>
public interface ICarSheetReader
{
    List<CarSheetRow> Read(Stream xlsx, int maxRows);
    byte[] BuildTemplate();
}

public class CarImportService
{
    public const long MaxBytes = 2 * 1024 * 1024;
    public const int MaxRows = 500;
    public static readonly string[] Columns =
    [
        "CarType", "Brand", "Model", "LicensePlate", "Seats", "CarAgeYears", "BasePricePerDay", "Description",
        "Transmission", "FuelType", "ManufactureYear", "SecurityDeposit", "BranchId", "PricePerHour"
    ];

    private readonly ICarSheetReader _reader;
    private readonly ICarRepository _cars;
    private readonly IUnitOfWork _uow;
    private readonly IPricingRepository? _pricing;

    public CarImportService(ICarSheetReader reader, ICarRepository cars, IUnitOfWork uow, IPricingRepository? pricing = null)
    {
        _reader = reader;
        _cars = cars;
        _uow = uow;
        _pricing = pricing;
    }

    public byte[] Template() => _reader.BuildTemplate();

    /// <summary>Tất cả hoặc không: có lỗi thì không ghi dòng nào. Staff nhập thì xe ChoDuyet, Admin thì SanSang.</summary>
    public async Task<ImportResultDto> ImportAsync(Stream file, long length, string fileName, bool dryRun, bool isAdmin)
    {
        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("IMPORT_INVALID_FILE", "Chỉ nhận tệp .xlsx.");
        if (length <= 0 || length > MaxBytes)
            throw new BadRequestException("IMPORT_FILE_TOO_LARGE", "Tệp phải có kích thước từ 1 byte đến 2 MB.");

        var rows = _reader.Read(file, MaxRows);
        var errors = new List<ImportRowError>();
        var cars = new List<CarEntity>();
        var plates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var branches = _pricing is null ? [] : await _pricing.ListBranchesAsync(false);

        foreach (var row in rows)
        {
            var before = errors.Count;
            void Err(string col, string msg) => errors.Add(new ImportRowError(row.Row, col, msg));
            string? Cell(string col) => row.Cells.TryGetValue(col, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

            T? Enum<T>(string col) where T : struct, System.Enum
            {
                var v = Cell(col);
                if (v is null) return null;
                if (TryParseEnum<T>(v, out var e)) return e;
                Err(col, $"Giá trị '{v}' không hợp lệ (nhập số hoặc nhãn: {string.Join(", ", System.Enum.GetNames<T>())}).");
                return null;
            }
            int? Int(string col)
            {
                var v = Cell(col);
                if (v is null) return null;
                if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return n;
                Err(col, $"'{v}' không phải số nguyên."); return null;
            }
            decimal? Dec(string col)
            {
                var v = Cell(col);
                if (v is null) return null;
                if (decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var n)) return n;
                Err(col, $"'{v}' không phải số."); return null;
            }

            var carType = Enum<CarType>("CarType");
            if (carType is null && Cell("CarType") is null) Err("CarType", "Bắt buộc nhập loại xe.");
            var seats = Int("Seats"); var age = Int("CarAgeYears"); var year = Int("ManufactureYear");
            var basePrice = Dec("BasePricePerDay"); var deposit = Dec("SecurityDeposit"); var perHour = Dec("PricePerHour");
            if (seats is null && Cell("Seats") is null) Err("Seats", "Bắt buộc nhập số chỗ.");
            if (basePrice is null && Cell("BasePricePerDay") is null) Err("BasePricePerDay", "Bắt buộc nhập giá theo ngày.");
            var plate = Cell("LicensePlate");

            int? branchId = null;
            if (Cell("BranchId") is { } b)
            {
                var found = int.TryParse(b, out var bid)
                    ? branches.FirstOrDefault(x => x.Id == bid)
                    : branches.FirstOrDefault(x => string.Equals(x.Name, b, StringComparison.OrdinalIgnoreCase));
                if (found is null) Err("BranchId", $"Chi nhánh '{b}' không tồn tại hoặc đã ngừng hoạt động.");
                else branchId = found.Id;
            }

            if (!isAdmin && deposit is > 0) Err("SecurityDeposit", "Chỉ quản trị viên được đặt tiền cọc xe.");
            if (!isAdmin && perHour.HasValue) Err("PricePerHour", "Chỉ quản trị viên được đặt giá thuê theo giờ.");
            if ((Cell("Brand")?.Length ?? 0) > 100) Err("Brand", "Tối đa 100 ký tự.");
            if ((Cell("Model")?.Length ?? 0) > 100) Err("Model", "Tối đa 100 ký tự.");
            if ((plate?.Length ?? 0) > 20) Err("LicensePlate", "Tối đa 20 ký tự.");
            if ((Cell("Description")?.Length ?? 0) > 2000) Err("Description", "Tối đa 2000 ký tự.");

            var request = new CreateCarRequest(
                carType ?? default, Cell("Brand") ?? "", Cell("Model") ?? "", plate ?? "", seats ?? 0, age ?? 0, basePrice ?? 0,
                Cell("Description"), Enum<Transmission>("Transmission"), Enum<FuelType>("FuelType"), year, deposit, branchId, perHour);

            // Chỉ kiểm các cột đã đọc được; cột lỗi cú pháp đã báo ở trên.
            foreach (var e in ValidateRequest(request))
                if (!errors.Any(x => x.Row == row.Row && x.Column == e.Column)) errors.Add(e with { Row = row.Row });

            if (plate is not null)
            {
                if (!plates.Add(plate)) Err("LicensePlate", $"Biển số '{plate}' bị trùng trong tệp.");
                else if (await _cars.GetByLicensePlateAsync(plate) is not null) Err("LicensePlate", $"Biển số '{plate}' đã tồn tại.");
            }

            if (errors.Count == before)
                cars.Add(new CarEntity
                {
                    CarType = request.CarType, Brand = request.Brand, Model = request.Model, LicensePlate = request.LicensePlate,
                    Seats = request.Seats, CarAgeYears = request.CarAgeYears, BasePricePerDay = request.BasePricePerDay,
                    Description = request.Description, Transmission = request.Transmission, FuelType = request.FuelType,
                    ManufactureYear = request.ManufactureYear, SecurityDeposit = request.SecurityDeposit ?? 0,
                    BranchId = request.BranchId, PricePerHour = request.PricePerHour,
                    Status = isAdmin ? CarStatus.SanSang : CarStatus.ChoDuyet
                });
        }

        var result = new ImportResultDto(rows.Count, cars.Count, errors);
        if (dryRun || errors.Count > 0 || cars.Count == 0) return result;

        foreach (var car in cars) await _cars.AddAsync(car);
        try
        {
            await _uow.SaveChangesAsync(); // một lần lưu = một transaction
        }
        catch (Exception) when (cars.Count > 0)
        {
            // Application không tham chiếu EF: coi là đụng unique biển số khi có biển số vừa bị tạo song song, ngược lại ném lại.
            var clash = new List<ImportRowError>();
            foreach (var row in rows)
                if (row.Cells.TryGetValue("LicensePlate", out var pl) && !string.IsNullOrWhiteSpace(pl)
                    && cars.Any(c => string.Equals(c.LicensePlate, pl.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    // Tách entity khỏi lần lưu hỏng không cần thiết: kiểm lại DB bằng truy vấn mới.
                    if (await _cars.GetByLicensePlateAsync(pl.Trim()) is { } ex && !cars.Contains(ex))
                        clash.Add(new ImportRowError(row.Row, "LicensePlate", $"Biển số '{pl.Trim()}' đã tồn tại."));
                }
            if (clash.Count == 0) throw;
            return new ImportResultDto(rows.Count, 0, clash);
        }
        return result with { Committed = true };
    }

    /// <summary>Dùng lại các attribute trên tham số của CreateCarRequest (Validator.TryValidateObject không đọc được attribute của record vị trí).</summary>
    private static IEnumerable<ImportRowError> ValidateRequest(CreateCarRequest r)
    {
        var ctor = typeof(CreateCarRequest).GetConstructors()[0];
        foreach (var p in ctor.GetParameters())
        {
            var attrs = p.GetCustomAttributes(typeof(ValidationAttribute), true).Cast<ValidationAttribute>().ToArray();
            if (attrs.Length == 0) continue;
            var value = typeof(CreateCarRequest).GetProperty(p.Name!)!.GetValue(r);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateValue(value!, new ValidationContext(r) { MemberName = p.Name, DisplayName = p.Name }, results, attrs))
                foreach (var res in results) yield return new ImportRowError(0, p.Name!, res.ErrorMessage ?? "Không hợp lệ.");
        }
    }

    /// <summary>Nhận số hoặc nhãn, không phân biệt hoa thường/dấu/khoảng trắng ("Số tự động" = SoTuDong).</summary>
    private static bool TryParseEnum<T>(string v, out T result) where T : struct, System.Enum
    {
        result = default;
        if (int.TryParse(v, out var n)) { if (!System.Enum.IsDefined(typeof(T), n)) return false; result = (T)System.Enum.ToObject(typeof(T), n); return true; }
        var key = Fold(v);
        foreach (var e in System.Enum.GetValues<T>())
            if (Fold(e.ToString()) == key) { result = e; return true; }
        return false;
    }

    private static string Fold(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || char.IsWhiteSpace(c)) continue;
            sb.Append(c == 'đ' || c == 'Đ' ? 'd' : char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
