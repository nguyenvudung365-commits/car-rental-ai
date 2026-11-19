using CarRental.Application.Common;
using CarRental.Application.Modules.Cars;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;
using Moq;

namespace CarRental.Tests.Unit;

/// <summary>Đợt 1: Staff tạo xe chờ duyệt, quyền giá, duyệt xe. Theo kế hoạch .claude/plans/car-rental-features-plan.md.</summary>
public class CarServiceWave1Tests
{
    private readonly Mock<ICarRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private CarService Svc() => new(_repo.Object, Mock.Of<IFileStorageService>(), _uow.Object);

    private static Car NewCar(CarStatus status, decimal price = 700_000, decimal? over = null) => new()
    {
        Id = 7, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-1", Seats = 5, CarAgeYears = 1,
        BasePricePerDay = price, OverridePrice = over, Status = status, CarType = CarType.Sedan
    };

    private static UpdateCarRequest Upd(Car c, decimal? price = null, decimal? over = null, CarStatus? status = null, string? desc = null) => new(
        c.CarType, c.Brand, c.Model, c.LicensePlate, c.Seats, c.CarAgeYears,
        price ?? c.BasePricePerDay, status ?? c.Status, over ?? c.OverridePrice, desc ?? c.Description);

    private void Setup(Car car)
    {
        _repo.Setup(r => r.GetByIdAsync(car.Id, It.IsAny<bool>())).ReturnsAsync(car);
        _repo.Setup(r => r.GetByLicensePlateAsync(It.IsAny<string>())).ReturnsAsync((Car?)null);
    }

    private static CreateCarRequest Create() => new(CarType.Sedan, "Toyota", "Vios", "51A-9", 5, 1, 700_000, null);

    [Theory]
    [InlineData(false, CarStatus.ChoDuyet)]
    [InlineData(true, CarStatus.SanSang)]
    public async Task Create_StatusDependsOnRole(bool isAdmin, CarStatus expected)
    {
        _repo.Setup(r => r.GetByLicensePlateAsync(It.IsAny<string>())).ReturnsAsync((Car?)null);
        Car? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Car>())).Callback<Car>(c => saved = c).Returns(Task.CompletedTask);

        var dto = await Svc().CreateAsync(Create(), isAdmin);

        Assert.Equal(expected, dto.Status);
        Assert.Equal(expected, saved!.Status);
    }

    [Fact]
    public async Task GetById_Pending_HiddenFromPublic_VisibleToStaff()
    {
        _repo.Setup(r => r.GetByIdAsync(7, true)).ReturnsAsync(NewCar(CarStatus.ChoDuyet));
        Assert.Null(await Svc().GetByIdAsync(7, includePending: false));
        Assert.NotNull(await Svc().GetByIdAsync(7, includePending: true));
    }

    [Fact]
    public async Task GetFiltered_IncludePendingIsSetByServiceNotByQuery()
    {
        CarFilterRequest? seen = null;
        _repo.Setup(r => r.GetFilteredAsync(It.IsAny<CarFilterRequest>()))
            .Callback<CarFilterRequest>(f => seen = f).ReturnsAsync((new List<Car>(), 0));
        // khách gửi IncludePending=true qua query thì vẫn bị ghi đè thành false
        await Svc().GetFilteredAsync(new CarFilterRequest(null, null, null, null, IncludePending: true), includePending: false);
        Assert.False(seen!.IncludePending);
        await Svc().GetFilteredAsync(new CarFilterRequest(null, null, null, null), includePending: true);
        Assert.True(seen!.IncludePending);
    }

    [Fact]
    public async Task Update_Staff_ChangeBasePrice_Forbidden()
    {
        var car = NewCar(CarStatus.SanSang); Setup(car);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => Svc().UpdateAsync(7, Upd(car, price: 800_000), isAdmin: false));
        Assert.Equal("PRICE_ADMIN_ONLY", ex.Code);
    }

    [Fact]
    public async Task Update_Staff_ChangeOverridePrice_Forbidden()
    {
        var car = NewCar(CarStatus.SanSang, over: 650_000); Setup(car);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => Svc().UpdateAsync(7, Upd(car, over: 600_000), false));
        Assert.Equal("PRICE_ADMIN_ONLY", ex.Code);
        var ex2 = await Assert.ThrowsAsync<ForbiddenException>(() => Svc().UpdateAsync(7, Upd(car) with { OverridePrice = null }, false));
        Assert.Equal("PRICE_ADMIN_ONLY", ex2.Code);
    }

    [Fact]
    public async Task Update_Staff_KeepPrices_EditDescription_Passes()
    {
        var car = NewCar(CarStatus.SanSang, over: 650_000); Setup(car);
        await Svc().UpdateAsync(7, Upd(car, desc: "mới"), false);
        Assert.Equal("mới", car.Description);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_Staff_MoveIntoPending_Forbidden()
    {
        var car = NewCar(CarStatus.SanSang); Setup(car);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => Svc().UpdateAsync(7, Upd(car, status: CarStatus.ChoDuyet), false));
        Assert.Equal("CAR_APPROVAL_ADMIN_ONLY", ex.Code);
    }

    [Fact]
    public async Task Update_Staff_MoveOutOfPending_Forbidden()
    {
        var car = NewCar(CarStatus.ChoDuyet); Setup(car);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => Svc().UpdateAsync(7, Upd(car, status: CarStatus.SanSang), false));
        Assert.Equal("CAR_APPROVAL_ADMIN_ONLY", ex.Code);
    }

    [Fact]
    public async Task Update_Staff_PendingCarStaysPending_Passes()
    {
        var car = NewCar(CarStatus.ChoDuyet); Setup(car);
        await Svc().UpdateAsync(7, Upd(car, desc: "x"), false);
        Assert.Equal(CarStatus.ChoDuyet, car.Status);
    }

    [Fact]
    public async Task Update_Admin_ChangePriceAndStatus_Passes()
    {
        var car = NewCar(CarStatus.ChoDuyet); Setup(car);
        await Svc().UpdateAsync(7, Upd(car, price: 900_000, over: 850_000, status: CarStatus.SanSang), true);
        Assert.Equal(900_000, car.BasePricePerDay);
        Assert.Equal(850_000, car.OverridePrice);
        Assert.Equal(CarStatus.SanSang, car.Status);
    }

    [Theory]
    [InlineData(CarStatus.SanSang)]
    [InlineData(CarStatus.DangThue)]
    [InlineData(CarStatus.BaoTri)]
    [InlineData(CarStatus.NgungHoatDong)]
    public async Task Approve_NotPending_Conflict(CarStatus status)
    {
        Setup(NewCar(status));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().ApproveAsync(7, null));
        Assert.Equal("CAR_NOT_PENDING", ex.Code);
    }

    [Fact]
    public async Task Approve_NotFound() => await Assert.ThrowsAsync<NotFoundException>(() => Svc().ApproveAsync(999, null));

    [Theory]
    [InlineData(0.0, null)]
    [InlineData(-5.0, null)]
    [InlineData(null, 0.0)]
    [InlineData(null, -1.0)]
    public async Task Approve_InvalidPrice_BadRequest(double? basePrice, double? over)
    {
        var car = NewCar(CarStatus.ChoDuyet); Setup(car);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Svc().ApproveAsync(7, new ApproveCarRequest((decimal?)basePrice, (decimal?)over)));
        Assert.Equal("INVALID_PRICE", ex.Code);
        Assert.Equal(CarStatus.ChoDuyet, car.Status);
        Assert.Equal(700_000, car.BasePricePerDay);
    }

    [Fact]
    public async Task Approve_WithoutPrice_KeepsPriceAndGoesSanSang()
    {
        var car = NewCar(CarStatus.ChoDuyet); Setup(car);
        var dto = await Svc().ApproveAsync(7, null);
        Assert.Equal(CarStatus.SanSang, dto.Status);
        Assert.Equal(700_000, car.BasePricePerDay);
        var car2 = NewCar(CarStatus.ChoDuyet); Setup(car2);
        await Svc().ApproveAsync(7, new ApproveCarRequest());
        Assert.Equal(CarStatus.SanSang, car2.Status);
        Assert.Equal(700_000, car2.BasePricePerDay);
    }

    [Fact]
    public async Task Approve_WithPrice_AppliesPrices()
    {
        var car = NewCar(CarStatus.ChoDuyet); Setup(car);
        var dto = await Svc().ApproveAsync(7, new ApproveCarRequest(950_000, 900_000));
        Assert.Equal(CarStatus.SanSang, dto.Status);
        Assert.Equal(950_000, dto.BasePricePerDay);
        Assert.Equal(900_000, dto.OverridePrice);
    }
}
