using System.Reflection;
using CarRental.API.Controllers;
using CarRental.Application.Common;
using CarRental.Application.Modules.Bookings;
using CarRental.Application.Modules.Cars;
using CarRental.Application.Modules.Reports;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;
using CarRental.Infrastructure.Reports;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Moq;

namespace CarRental.Tests.Unit;

/// <summary>Đợt 5: MaintenanceService (mock), CalendarService, CarImportService + CarSheetReader, ReportExporter, quyền theo attribute.</summary>
public class Wave5MaintenanceServiceTests
{
    private readonly Mock<IMaintenanceRepository> _repo = new();
    private readonly Mock<ICarRepository> _cars = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Car _car = new() { Id = 7, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-1", Status = CarStatus.SanSang };

    private MaintenanceService Svc()
    {
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<int>> f, CancellationToken _) => f());
        _cars.Setup(c => c.GetByIdAsync(7, false)).ReturnsAsync(_car);
        _repo.Setup(r => r.GetCarIdAsync(1)).ReturnsAsync(_car.Id);
        _repo.Setup(r => r.FindOverlappingBookingIdsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<int>());
        return new MaintenanceService(_repo.Object, _cars.Object, _uow.Object);
    }

    private CarMaintenance Item(MaintenanceStatus s, int id = 1) => new()
    { Id = id, CarId = 7, Car = _car, Status = s, DueDate = DateTime.UtcNow.Date, Cost = 123_000 };

    [Fact]
    public async Task Start_CarRenting_Throws_CAR_IN_USE()
    {
        _car.Status = CarStatus.DangThue;
        var item = Item(MaintenanceStatus.KeHoach);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().StartAsync(1, true, true));
        Assert.Equal("CAR_IN_USE", ex.Code);
        Assert.Equal(MaintenanceStatus.KeHoach, item.Status);
    }

    [Fact]
    public async Task Start_WithOverlap_Throws_UnlessForce()
    {
        var item = Item(MaintenanceStatus.KeHoach);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        var svc = Svc();
        _repo.Setup(r => r.FindOverlappingBookingIdsAsync(7, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<int> { 5 });
        var ex = await Assert.ThrowsAsync<ConflictException>(() => svc.StartAsync(1, false, true));
        Assert.Equal("MAINTENANCE_BOOKING_OVERLAP", ex.Code);
        Assert.Equal(CarStatus.SanSang, _car.Status);

        var dto = await svc.StartAsync(1, true, true);
        Assert.Equal(MaintenanceStatus.DangThucHien, dto.Status);
        Assert.Equal(CarStatus.BaoTri, _car.Status);
        Assert.NotNull(item.StartedAt);
    }

    [Theory]
    [InlineData(CarStatus.NgungHoatDong)]
    [InlineData(CarStatus.ChoDuyet)]
    public async Task Start_InactiveOrPendingCar_Throws(CarStatus st)
    {
        _car.Status = st;
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Item(MaintenanceStatus.KeHoach));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().StartAsync(1, true, true));
        Assert.Equal("CAR_NOT_AVAILABLE", ex.Code);
    }

    [Theory]
    [InlineData(MaintenanceStatus.DangThucHien)]
    [InlineData(MaintenanceStatus.HoanThanh)]
    [InlineData(MaintenanceStatus.Huy)]
    public async Task Start_NotPlanned_Throws_State(MaintenanceStatus s)
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Item(s));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().StartAsync(1, false, true));
        Assert.Equal("MAINTENANCE_STATE", ex.Code);
    }

    [Fact]
    public async Task Start_NotFound_Throws() => await Assert.ThrowsAsync<NotFoundException>(() => Svc().StartAsync(99, false, true));

    [Fact]
    public async Task Complete_CarInMaintenance_NoOtherInProgress_ReturnsToSanSang()
    {
        _car.Status = CarStatus.BaoTri;
        var item = Item(MaintenanceStatus.DangThucHien);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _repo.Setup(r => r.HasOtherInProgressAsync(7, 1)).ReturnsAsync(false);
        var dto = await Svc().CompleteAsync(1, new CompleteMaintenanceRequest(50_000, "ok", null), true);
        Assert.Equal(MaintenanceStatus.HoanThanh, dto.Status);
        Assert.Equal(CarStatus.SanSang, _car.Status);
        Assert.Equal(50_000, item.Cost);
    }

    [Fact]
    public async Task Complete_OtherScheduleStillInProgress_KeepsBaoTri()
    {
        _car.Status = CarStatus.BaoTri;
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Item(MaintenanceStatus.DangThucHien));
        _repo.Setup(r => r.HasOtherInProgressAsync(7, 1)).ReturnsAsync(true);
        await Svc().CompleteAsync(1, new CompleteMaintenanceRequest(null, null, null), true);
        Assert.Equal(CarStatus.BaoTri, _car.Status);
    }

    [Theory]
    [InlineData(CarStatus.NgungHoatDong)]
    [InlineData(CarStatus.DangThue)]
    [InlineData(CarStatus.SanSang)]
    public async Task Complete_CarNotInBaoTri_StatusUntouched(CarStatus st)
    {
        _car.Status = st;
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Item(MaintenanceStatus.DangThucHien));
        _repo.Setup(r => r.HasOtherInProgressAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);
        await Svc().CompleteAsync(1, new CompleteMaintenanceRequest(null, null, null), true);
        Assert.Equal(st, _car.Status);
    }

    [Fact]
    public async Task Complete_NotInProgress_Throws_State()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Item(MaintenanceStatus.KeHoach));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().CompleteAsync(1, new CompleteMaintenanceRequest(null, null, null), true));
        Assert.Equal("MAINTENANCE_STATE", ex.Code);
    }

    [Fact]
    public async Task Complete_NextDueDateTodayOrPast_IsValidationError()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Item(MaintenanceStatus.DangThucHien));
        await Assert.ThrowsAsync<ValidationAppException>(() =>
            Svc().CompleteAsync(1, new CompleteMaintenanceRequest(null, null, DateTime.UtcNow.Date), true));
    }

    [Fact]
    public async Task Complete_NextDueDate_AddsFollowUpPlan()
    {
        _car.Status = CarStatus.BaoTri;
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Item(MaintenanceStatus.DangThucHien));
        var next = DateTime.UtcNow.Date.AddDays(180);
        await Svc().CompleteAsync(1, new CompleteMaintenanceRequest(null, null, next), true);
        _repo.Verify(r => r.AddAsync(It.Is<CarMaintenance>(m => m.DueDate == next && m.CarId == 7 && m.Status == MaintenanceStatus.KeHoach)), Times.Once);
    }

    [Fact]
    public async Task Cost_OnlyVisibleToAdmin()
    {
        var item = Item(MaintenanceStatus.KeHoach);
        _repo.Setup(r => r.ListAsync(null, null)).ReturnsAsync(new List<CarMaintenance> { item });
        var svc = Svc();
        Assert.Equal(123_000, (await svc.ListAsync(new MaintenanceListFilter(null, null), true))[0].Cost);
        Assert.Null((await svc.ListAsync(new MaintenanceListFilter(null, null), false))[0].Cost);
    }

    [Fact]
    public async Task Cost_HiddenFromStaffOnComplete()
    {
        _car.Status = CarStatus.BaoTri;
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Item(MaintenanceStatus.DangThucHien));
        var dto = await Svc().CompleteAsync(1, new CompleteMaintenanceRequest(900, null, null), false);
        Assert.Null(dto.Cost);
    }

    [Fact]
    public async Task Cancel_OnlyPlanned()
    {
        var planned = Item(MaintenanceStatus.KeHoach);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(planned);
        _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Item(MaintenanceStatus.DangThucHien, 2));
        var svc = Svc();
        Assert.Equal(MaintenanceStatus.Huy, (await svc.CancelAsync(1, false)).Status);
        await Assert.ThrowsAsync<ConflictException>(() => svc.CancelAsync(2, false));
    }

    [Fact]
    public async Task Create_UnknownType_And_UnknownCar_Fail()
    {
        var svc = Svc();
        await Assert.ThrowsAsync<BadRequestException>(() => svc.CreateAsync(new CreateMaintenanceRequest(7, (MaintenanceType)99, DateTime.UtcNow, null, null), true));
        _cars.Setup(c => c.GetByIdAsync(404, false)).ReturnsAsync((Car?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => svc.CreateAsync(new CreateMaintenanceRequest(404, MaintenanceType.DangKiem, DateTime.UtcNow, null, null), true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task Due_WithinDaysOutOfRange_Throws(int d)
        => await Assert.ThrowsAsync<ValidationAppException>(() => Svc().GetDueAsync(d, true));

    [Fact]
    public async Task Due_ClassifiesOverdueUpcomingAndKm_ComputedAtReadTime()
    {
        var today = DateTime.UtcNow.Date;
        CarMaintenance P(int id, int days, int? km = null, int carId = 7) => new()
        { Id = id, CarId = carId, Car = _car, DueDate = today.AddDays(days), DueOdometer = km, Type = MaintenanceType.DangKiem };
        var planned = new List<CarMaintenance>
        {
            P(1, -1), P(2, 0), P(3, 30), P(4, 31),
            P(5, 200, 10_000, 7),   // km 9501 -> gần mốc (còn 499)
            P(6, 200, 10_000, 8),   // km 9500 -> còn đúng 500: không nhắc (< 500)
            P(7, 200, 10_000, 9),   // km 12000 -> đã quá mốc
            P(8, 200, 10_000, 10),  // không có biên bản
        };
        _repo.Setup(r => r.ListPlannedAsync()).ReturnsAsync(planned);
        _repo.Setup(r => r.GetLatestOdometersAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [7] = 9501, [8] = 9500, [9] = 12_000 });

        var due = await Svc().GetDueAsync(30, true);
        Assert.Equal([1], due.Overdue.Select(x => x.Id));
        Assert.Equal([2, 3], due.Upcoming.Select(x => x.Id).Order());
        Assert.Equal([5, 7], due.ByOdometer.Select(x => x.Id).Order());
    }
}

public class Wave5CalendarServiceTests
{
    private static CalendarService Svc() => new(Mock.Of<ICalendarQuery>(q => q.GetAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()) == Task.FromResult(new List<CarCalendarDto>())));

    [Fact]
    public async Task Range_ToNotAfterFrom_Throws()
    {
        var d = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await Assert.ThrowsAsync<ValidationAppException>(() => Svc().GetAsync(d, d, null));
        await Assert.ThrowsAsync<ValidationAppException>(() => Svc().GetAsync(d, d.AddDays(-1), null));
    }

    [Fact]
    public async Task Range_Max62Days_Boundary()
    {
        var d = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Empty(await Svc().GetAsync(d, d.AddDays(62), null));
        await Assert.ThrowsAsync<ValidationAppException>(() => Svc().GetAsync(d, d.AddDays(62).AddSeconds(1), null));
    }
}

public class Wave5ImportTests
{
    private static readonly string[] Cols = CarImportService.Columns;

    private static MemoryStream Sheet(params Dictionary<string, string>[] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Xe");
        for (var c = 0; c < Cols.Length; c++) ws.Cell(1, c + 1).SetValue(Cols[c]);
        for (var r = 0; r < rows.Length; r++)
            foreach (var (k, v) in rows[r])
            {
                var cell = ws.Cell(r + 2, Array.IndexOf(Cols, k) + 1);
                if (v.StartsWith('=')) cell.FormulaA1 = v[1..]; else cell.SetValue(v);
            }
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static Dictionary<string, string> Row(string plate, Action<Dictionary<string, string>>? tweak = null)
    {
        var d = new Dictionary<string, string>
        {
            ["CarType"] = "Sedan", ["Brand"] = "Toyota", ["Model"] = "Vios", ["LicensePlate"] = plate, ["Seats"] = "5",
            ["CarAgeYears"] = "2", ["BasePricePerDay"] = "800000"
        };
        tweak?.Invoke(d);
        return d;
    }

    private readonly Mock<ICarRepository> _cars = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly List<Car> _added = [];

    private CarImportService Svc()
    {
        _cars.Setup(c => c.AddAsync(It.IsAny<Car>())).Callback<Car>(_added.Add).Returns(Task.CompletedTask);
        return new CarImportService(new CarSheetReader(), _cars.Object, _uow.Object);
    }

    private Task<ImportResultDto> Run(Stream s, bool dry = false, bool admin = true, string name = "x.xlsx", long? len = null)
        => Svc().ImportAsync(s, len ?? s.Length, name, dry, admin);

    [Fact]
    public async Task Admin_ValidRows_CommitAsSanSang_SingleSave()
    {
        var r = await Run(Sheet(Row("A-1"), Row("A-2", d => { d["CarType"] = "2"; d["Transmission"] = "Số tự động"; d["FuelType"] = "dien"; })));
        Assert.True(r.Committed);
        Assert.Equal(2, r.Valid);
        Assert.All(_added, c => Assert.Equal(CarStatus.SanSang, c.Status));
        Assert.Equal(CarType.SUV, _added[1].CarType);
        Assert.Equal(Transmission.SoTuDong, _added[1].Transmission);
        Assert.Equal(FuelType.Dien, _added[1].FuelType);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Staff_Rows_BecomeChoDuyet()
    {
        var r = await Run(Sheet(Row("S-1")), admin: false);
        Assert.True(r.Committed);
        Assert.Equal(CarStatus.ChoDuyet, _added.Single().Status);
    }

    [Fact]
    public async Task Staff_DepositAndPricePerHour_Blocked_ZeroDepositAllowed()
    {
        var r = await Run(Sheet(
            Row("S-1", d => d["SecurityDeposit"] = "1000000"),
            Row("S-2", d => d["PricePerHour"] = "100000"),
            Row("S-3", d => d["SecurityDeposit"] = "0")), admin: false);
        Assert.False(r.Committed);
        Assert.Contains(r.Errors, e => e.Row == 2 && e.Column == "SecurityDeposit");
        Assert.Contains(r.Errors, e => e.Row == 3 && e.Column == "PricePerHour");
        Assert.DoesNotContain(r.Errors, e => e.Row == 4);
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Admin_CanSetDepositAndPricePerHour()
    {
        var r = await Run(Sheet(Row("A-1", d => { d["SecurityDeposit"] = "5000000"; d["PricePerHour"] = "120000"; })));
        Assert.True(r.Committed);
        Assert.Equal(5_000_000, _added[0].SecurityDeposit);
        Assert.Equal(120_000, _added[0].PricePerHour);
    }

    [Fact]
    public async Task AllOrNothing_OneBadRow_WritesNothing_ErrorsByRowAndColumn()
    {
        var r = await Run(Sheet(Row("A-1"), Row("A-2", d => d["Seats"] = "abc"), Row("A-3", d => d["CarType"] = "99")));
        Assert.False(r.Committed);
        Assert.Equal(3, r.Total);
        Assert.Equal(1, r.Valid);
        Assert.Contains(r.Errors, e => e.Row == 3 && e.Column == "Seats");
        Assert.Contains(r.Errors, e => e.Row == 4 && e.Column == "CarType");
        Assert.Empty(_added);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DryRun_ValidFile_DoesNotWrite()
    {
        var r = await Run(Sheet(Row("A-1")), dry: true);
        Assert.False(r.Committed);
        Assert.Equal(1, r.Valid);
        Assert.Empty(_added);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DuplicatePlate_InFile_AndInDb_AreRowErrors()
    {
        _cars.Setup(c => c.GetByLicensePlateAsync("DB-1")).ReturnsAsync(new Car { LicensePlate = "DB-1" });
        var r = await Run(Sheet(Row("N-1"), Row("n-1"), Row("DB-1")));
        Assert.Contains(r.Errors, e => e.Row == 3 && e.Column == "LicensePlate");
        Assert.Contains(r.Errors, e => e.Row == 4 && e.Column == "LicensePlate");
        Assert.False(r.Committed);
    }

    [Fact]
    public async Task MissingRequired_ReportedPerColumn()
    {
        var r = await Run(Sheet(Row("A-1", d => { d.Remove("CarType"); d.Remove("Seats"); d.Remove("BasePricePerDay"); })), dry: true);
        Assert.Contains(r.Errors, e => e.Column == "CarType");
        Assert.Contains(r.Errors, e => e.Column == "Seats");
        Assert.Contains(r.Errors, e => e.Column == "BasePricePerDay");
    }

    [Fact]
    public void Formula_DoesNotProduceEvaluatedNumber_InReader()
    {
        var rows = new CarSheetReader().Read(Sheet(Row("A-1", d => d["Seats"] = "=2+3")), 500);
        var seats = rows.Single().Cells["Seats"];
        Assert.NotEqual("5", seats);
    }

    [Fact]
    public async Task NotXlsx_And_Sizes_Rejected()
    {
        var svc = Svc();
        var ex1 = await Assert.ThrowsAsync<BadRequestException>(() => svc.ImportAsync(new MemoryStream([1]), 1, "a.csv", true, true));
        Assert.Equal("IMPORT_INVALID_FILE", ex1.Code);
        var ex2 = await Assert.ThrowsAsync<BadRequestException>(() => svc.ImportAsync(new MemoryStream([1]), CarImportService.MaxBytes + 1, "a.xlsx", true, true));
        Assert.Equal("IMPORT_FILE_TOO_LARGE", ex2.Code);
        await Assert.ThrowsAsync<BadRequestException>(() => svc.ImportAsync(new MemoryStream(), 0, "a.xlsx", true, true));
        // đúng 2 MB không bị từ chối vì kích thước (lỗi chỉ có thể là tệp hỏng)
        var ex3 = await Assert.ThrowsAsync<BadRequestException>(() => svc.ImportAsync(new MemoryStream([1]), CarImportService.MaxBytes, "a.xlsx", true, true));
        Assert.Equal("IMPORT_INVALID_FILE", ex3.Code);
    }

    [Fact]
    public async Task CorruptXlsx_IsBadRequest_NotCrash()
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Run(new MemoryStream("not a zip"u8.ToArray()), dry: true));
        Assert.Equal("IMPORT_INVALID_FILE", ex.Code);
    }

    [Fact]
    public async Task MaxRows_500_Ok_501_Rejected()
    {
        var ok = Sheet(Enumerable.Range(1, 500).Select(i => Row("P-" + i)).ToArray());
        Assert.Equal(500, (await Run(ok, dry: true)).Total);
        var many = Sheet(Enumerable.Range(1, 501).Select(i => Row("P-" + i)).ToArray());
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Run(many, dry: true));
        Assert.Equal("IMPORT_TOO_MANY_ROWS", ex.Code);
    }

    [Fact]
    public async Task EmptySheet_NoRows_NotCommitted()
    {
        var r = await Run(Sheet(), dry: false);
        Assert.False(r.Committed);
        Assert.Equal(0, r.Total);
    }

    [Fact]
    public async Task OutOfRange_Seats_And_LongPlate_AreErrors()
    {
        var r = await Run(Sheet(Row("A-1", d => d["Seats"] = "99"), Row(new string('X', 21))), dry: true);
        Assert.Contains(r.Errors, e => e.Row == 2 && e.Column == "Seats");
        Assert.Contains(r.Errors, e => e.Row == 3 && e.Column == "LicensePlate");
    }

    [Fact]
    public void Template_IsReadable_AndHasAllColumns()
    {
        var bytes = new CarSheetReader().BuildTemplate();
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var header = wb.Worksheets.First().Row(1).Cells(1, Cols.Length).Select(c => c.GetString()).ToArray();
        Assert.Equal(Cols, header);
    }
}

public class Wave5ReportExporterTests
{
    private static readonly ReportExporter Ex = new();

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+1", "'+1")]
    [InlineData("-1", "'-1")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("\tx", "'\tx")]
    [InlineData("\rx", "'\rx")]
    [InlineData("bình thường", "bình thường")]
    [InlineData("a=b", "a=b")]
    [InlineData("", "")]
    public void Safe_PrefixesFormulaStarters(string input, string expected) => Assert.Equal(expected, ReportExporter.Safe(input));

    [Fact]
    public void Safe_Null_IsEmpty() => Assert.Equal("", ReportExporter.Safe(null));

    private static BookingDto Bk(string customer, string? note) => new(
        1, 1, customer, 1, "=HYPERLINK(\"http://x\")", "@plate", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1, null, 1, 1, false,
        "ChoXacNhan", null, note, DateTime.UtcNow);

    [Fact]
    public void BookingsXlsx_NeutralisesInjectionInEveryTextCell()
    {
        var bytes = Ex.BookingsXlsx([Bk("=cmd|'/C calc'!A0", "+1+1")]);
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        foreach (var c in ws.Row(2).CellsUsed().Where(c => c.DataType == XLDataType.Text))
        {
            Assert.False(c.HasFormula, "Ô " + c.Address + " có công thức.");
            Assert.True(c.Style.IncludeQuotePrefix || !"=+-@\t\r".Contains(c.GetString()[0]), "O " + c.Address + " khong duoc vo hieu.");
        }
        Assert.Equal("=cmd|'/C calc'!A0", ws.Cell(2, 2).GetString()); // ClosedXML tách dấu ' thành quotePrefix
    }

    private static RevenueReportDto Rev() => new(
        1_500_000, 2,
        [new RevenueByMonthDto("2026-11", 1_500_000, 2)],
        [new RevenueByMethodDto(PaymentMethod.TienMat, 1_000_000, 1), new RevenueByMethodDto(PaymentMethod.ChuyenKhoan, 500_000, 1)]);

    [Fact]
    public void RevenueXlsx_HasMonthAndMethodSheets()
    {
        using var wb = new XLWorkbook(new MemoryStream(Ex.RevenueXlsx(Rev())));
        Assert.Equal(2, wb.Worksheets.Count);
        var m = wb.Worksheet("Theo phương thức");
        Assert.Equal("TienMat", m.Cell(2, 1).GetString());
        Assert.Equal(1_000_000, m.Cell(2, 2).GetDouble());
        Assert.Equal(1_500_000, wb.Worksheet("Theo tháng").Cell(2, 2).GetDouble());
    }

    [Fact]
    public void RevenueXlsx_NullByMethod_DoesNotThrow()
        => Assert.NotEmpty(Ex.RevenueXlsx(new RevenueReportDto(0, 0, [])));

    [Fact]
    public void Pdf_Revenue_And_Occupancy_StartWithPdfMagic()
    {
        var d = new DateTime(2026, 11, 1);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(Ex.RevenuePdf(Rev(), d, d.AddDays(5)), 0, 4));
        var occ = new OccupancyReportDto(1, 0.5, [new CarOccupancyDto(1, "51A-1", 0.5)]);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(Ex.OccupancyPdf(occ, d, d.AddDays(5)), 0, 4));
        using var wb = new XLWorkbook(new MemoryStream(Ex.OccupancyXlsx(occ)));
        Assert.Equal(0.5, wb.Worksheets.First().Cell(2, 2).GetDouble());
    }
}

public class Wave5ExportServiceTests
{
    [Fact]
    public async Task InvalidFormat_IsBadRequest()
    {
        var reports = new ReportService(Mock.Of<IReportRepository>());
        var svc = new ReportExportService(reports, Mock.Of<IBookingService>(), Mock.Of<IReportExporter>());
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => svc.RevenueAsync("csv", null, null));
        Assert.Equal("INVALID_FORMAT", ex.Code);
    }

    [Fact]
    public async Task Format_DefaultsToXlsx_PdfCaseInsensitive()
    {
        var repo = new Mock<IReportRepository>();
        repo.Setup(r => r.GetRevenueAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new RevenueReportDto(0, 0, []));
        var exp = new Mock<IReportExporter>();
        exp.Setup(e => e.RevenueXlsx(It.IsAny<RevenueReportDto>())).Returns([1]);
        exp.Setup(e => e.RevenuePdf(It.IsAny<RevenueReportDto>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([2]);
        var svc = new ReportExportService(new ReportService(repo.Object), Mock.Of<IBookingService>(), exp.Object);
        Assert.EndsWith(".xlsx", (await svc.RevenueAsync(null, null, null)).FileName);
        var pdf = await svc.RevenueAsync("PDF", null, null);
        Assert.Equal("application/pdf", pdf.ContentType);
        Assert.EndsWith(".pdf", pdf.FileName);
    }

    [Fact]
    public async Task Bookings_CappedAt5000Rows()
    {
        var bs = new Mock<IBookingService>();
        BookingDto B(int i) => new(i, 1, "c", 1, "car", "p", DateTime.UtcNow, DateTime.UtcNow, 1, null, 1, 1, false, "x", null, null, DateTime.UtcNow);
        bs.Setup(b => b.GetFilteredAsync(It.IsAny<BookingFilterRequest>()))
            .Returns((BookingFilterRequest f, int? _) => Task.FromResult((Enumerable.Range((f.Page - 1) * f.PageSize, f.PageSize).Select(B).ToList(), 9000)));
        IEnumerable<BookingDto>? got = null;
        var exp = new Mock<IReportExporter>();
        exp.Setup(e => e.BookingsXlsx(It.IsAny<IEnumerable<BookingDto>>())).Callback<IEnumerable<BookingDto>>(x => got = x.ToList()).Returns([1]);
        var svc = new ReportExportService(new ReportService(Mock.Of<IReportRepository>()), bs.Object, exp.Object);
        await svc.BookingsAsync(new BookingFilterRequest(null, null, null, null, null));
        Assert.Equal(ReportExportService.MaxBookingRows, got!.Count());
    }
}

public class Wave5AuthorizationAttributeTests
{
    private static string? Roles(Type controller, string action)
    {
        var m = controller.GetMethods().Single(x => x.Name == action && x.DeclaringType == controller);
        var attr = m.GetCustomAttribute<AuthorizeAttribute>() ?? controller.GetCustomAttribute<AuthorizeAttribute>();
        return attr is null ? null : string.Join(",", attr.Roles!.Split(',').OrderBy(x => x));
    }

    [Theory]
    [InlineData(typeof(MaintenanceController), "List", "Admin,Staff")]
    [InlineData(typeof(MaintenanceController), "Due", "Admin,Staff")]
    [InlineData(typeof(MaintenanceController), "Create", "Admin,Staff")]
    [InlineData(typeof(MaintenanceController), "Start", "Admin,Staff")]
    [InlineData(typeof(MaintenanceController), "Complete", "Admin,Staff")]
    [InlineData(typeof(MaintenanceController), "Cancel", "Admin,Staff")]
    [InlineData(typeof(CalendarController), "Get", "Admin,Staff")]
    [InlineData(typeof(DashboardController), "Summary", "Admin,Staff")]
    [InlineData(typeof(CarImportController), "Template", "Admin,Staff")]
    [InlineData(typeof(CarImportController), "Import", "Admin,Staff")]
    [InlineData(typeof(ReportExportsController), "Revenue", "Admin")]
    [InlineData(typeof(ReportExportsController), "Occupancy", "Admin")]
    [InlineData(typeof(ReportExportsController), "Bookings", "Admin,Staff")]
    public void Action_HasExpectedRoles(Type controller, string action, string expected)
        => Assert.Equal(expected, Roles(controller, action));
}
