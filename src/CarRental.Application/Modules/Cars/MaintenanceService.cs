using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;

namespace CarRental.Application.Modules.Cars;

public class MaintenanceService : IMaintenanceService
{
    /// <summary>Còn dưới ngưỡng này (km) so với mốc thì nhắc theo km.</summary>
    public const int OdometerWarnKm = 500;

    private readonly IMaintenanceRepository _repo;
    private readonly ICarRepository _cars;
    private readonly IUnitOfWork _uow;

    public MaintenanceService(IMaintenanceRepository repo, ICarRepository cars, IUnitOfWork uow)
    {
        _repo = repo;
        _cars = cars;
        _uow = uow;
    }

    public async Task<List<MaintenanceDto>> ListAsync(MaintenanceListFilter filter, bool isAdmin)
        => (await _repo.ListAsync(filter.CarId, filter.Status)).Select(m => ToDto(m, isAdmin)).ToList();

    public async Task<MaintenanceDto> CreateAsync(CreateMaintenanceRequest request, bool isAdmin)
    {
        if (!Enum.IsDefined(request.Type))
            throw new BadRequestException("INVALID_TYPE", "Loại lịch không hợp lệ.");
        var car = await _cars.GetByIdAsync(request.CarId) ?? throw new NotFoundException("Không tìm thấy xe.");
        var item = new CarMaintenance
        {
            CarId = car.Id,
            Car = car,
            Type = request.Type,
            DueDate = request.DueDate.Date,
            DueOdometer = request.DueOdometer,
            Note = request.Note
        };
        await _repo.AddAsync(item);
        await _uow.SaveChangesAsync();
        return ToDto(item, isAdmin);
    }

    public async Task<MaintenanceDueDto> GetDueAsync(int withinDays, bool isAdmin)
    {
        if (withinDays is < 1 or > 365) throw new ValidationAppException("withinDays phải từ 1 đến 365.");
        var today = DateTime.UtcNow.Date;
        var planned = await _repo.ListPlannedAsync();
        var overdue = planned.Where(m => m.DueDate.Date < today).OrderBy(m => m.DueDate).ToList();
        var upcoming = planned.Where(m => m.DueDate.Date >= today && m.DueDate.Date <= today.AddDays(withinDays)).OrderBy(m => m.DueDate).ToList();

        var withKm = planned.Where(m => m.DueOdometer.HasValue).ToList();
        var odo = await _repo.GetLatestOdometersAsync(withKm.Select(m => m.CarId).Distinct());
        var byKm = withKm.Where(m => odo.TryGetValue(m.CarId, out var km) && km > m.DueOdometer!.Value - OdometerWarnKm).ToList();

        return new MaintenanceDueDto(
            overdue.Select(m => ToDto(m, isAdmin)).ToList(),
            upcoming.Select(m => ToDto(m, isAdmin)).ToList(),
            byKm.Select(m => ToDto(m, isAdmin)).ToList());
    }

    public async Task<MaintenanceDto> StartAsync(int id, bool force, bool isAdmin)
    {
        var carId = await _repo.GetCarIdAsync(id) ?? throw new NotFoundException("Không tìm thấy lịch bảo trì.");
        CarMaintenance? item = null;
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            await _cars.LockForBookingAsync(carId);
            // Nạp (có tracking) sau khi khóa để trạng thái lịch và xe là mới nhất.
            item = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy lịch bảo trì.");
            var car = await _cars.GetByIdAsync(carId) ?? throw new NotFoundException("Không tìm thấy xe.");
            if (item.Status != MaintenanceStatus.KeHoach)
                throw new ConflictException("MAINTENANCE_STATE", "Chỉ bắt đầu được lịch đang ở trạng thái kế hoạch.");
            if (car.Status == CarStatus.DangThue)
                throw new ConflictException("CAR_IN_USE", "Xe đang được thuê, không thể bắt đầu bảo trì.");
            if (car.Status is not (CarStatus.SanSang or CarStatus.BaoTri))
                throw new ConflictException("CAR_NOT_AVAILABLE", "Xe đang ngừng hoạt động hoặc chờ duyệt.");

            var now = DateTime.UtcNow;
            var dayEnd = item.DueDate.Date.AddDays(1);
            var overlaps = await _repo.FindOverlappingBookingIdsAsync(car.Id, now, dayEnd > now ? dayEnd : now.AddDays(1));
            if (overlaps.Count > 0 && !force)
                throw new ConflictException("MAINTENANCE_BOOKING_OVERLAP",
                    $"Có {overlaps.Count} đơn còn hiệu lực trùng thời gian bảo trì (mã đơn: {string.Join(", ", overlaps)}). Gửi force=true để vẫn bắt đầu.");

            item.Status = MaintenanceStatus.DangThucHien;
            item.StartedAt = now;
            car.Status = CarStatus.BaoTri;
            _cars.Update(car);
            await _uow.SaveChangesAsync();
            return 0;
        });
        return ToDto(item!, isAdmin);
    }

    public async Task<MaintenanceDto> CompleteAsync(int id, CompleteMaintenanceRequest request, bool isAdmin)
    {
        var carId = await _repo.GetCarIdAsync(id) ?? throw new NotFoundException("Không tìm thấy lịch bảo trì.");
        CarMaintenance? item = null;
        var now = DateTime.UtcNow;
        if (request.NextDueDate is { } next && next.Date <= now.Date)
            throw new ValidationAppException("NEXT_DUE_INVALID", "Ngày hạn kế tiếp phải sau hôm nay.");

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            await _cars.LockForBookingAsync(carId);
            item = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy lịch bảo trì.");
            var car = await _cars.GetByIdAsync(carId) ?? throw new NotFoundException("Không tìm thấy xe.");
            if (item.Status != MaintenanceStatus.DangThucHien)
                throw new ConflictException("MAINTENANCE_STATE", "Chỉ hoàn thành được lịch đang thực hiện.");

            item.Status = MaintenanceStatus.HoanThanh;
            item.CompletedAt = now;
            if (request.Cost.HasValue) item.Cost = request.Cost;
            if (!string.IsNullOrWhiteSpace(request.Note)) item.Note = request.Note;

            // Chỉ trả xe về sẵn sàng khi đang bảo trì và không còn lịch khác đang làm; không ghi đè NgungHoatDong.
            if (car.Status == CarStatus.BaoTri && !await _repo.HasOtherInProgressAsync(car.Id, item.Id))
            {
                car.Status = CarStatus.SanSang;
                _cars.Update(car);
            }
            if (request.NextDueDate is { } nd)
                await _repo.AddAsync(new CarMaintenance { CarId = car.Id, Type = item.Type, DueDate = nd.Date });
            await _uow.SaveChangesAsync();
            return 0;
        });
        return ToDto(item!, isAdmin);
    }

    public async Task<MaintenanceDto> CancelAsync(int id, bool isAdmin)
    {
        var item = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy lịch bảo trì.");
        if (item.Status != MaintenanceStatus.KeHoach)
            throw new ConflictException("MAINTENANCE_STATE", "Chỉ hủy được lịch đang ở trạng thái kế hoạch.");
        item.Status = MaintenanceStatus.Huy;
        await _uow.SaveChangesAsync();
        return ToDto(item, isAdmin);
    }

    private static MaintenanceDto ToDto(CarMaintenance m, bool isAdmin) => new(
        m.Id, m.CarId, m.Car is null ? "" : $"{m.Car.Brand} {m.Car.Model}", m.Car?.LicensePlate ?? "", m.Type, m.DueDate,
        m.DueOdometer, m.StartedAt, m.CompletedAt, isAdmin ? m.Cost : null, m.Note, m.Status);
}
