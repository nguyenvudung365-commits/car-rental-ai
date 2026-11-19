using CarRental.Application.Common;
using CarRental.Application.Modules.Pricing;
using CarRental.Domain.Common;
using Microsoft.AspNetCore.Http;
using CarEntity = CarRental.Domain.Modules.Cars.Car;
using CarImageEntity = CarRental.Domain.Modules.Cars.CarImage;

namespace CarRental.Application.Modules.Cars;

public class CarService : ICarService
{
    private readonly ICarRepository _carRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingRepository? _pricing;

    public CarService(ICarRepository carRepository, IFileStorageService fileStorageService, IUnitOfWork unitOfWork, IPricingRepository? pricing = null)
    {
        _pricing = pricing;
        _carRepository = carRepository;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<(List<CarDto> Items, int TotalCount)> GetFilteredAsync(CarFilterRequest filter, bool includePending = false)
    {
        if (filter.AvailableFrom.HasValue != filter.AvailableTo.HasValue)
            throw new ValidationAppException("Cần nhập đủ cả ngày nhận và ngày trả khi lọc xe còn trống.");
        if (filter.AvailableFrom.HasValue && filter.AvailableTo <= filter.AvailableFrom)
            throw new ValidationAppException("Ngày trả phải sau ngày nhận.");

        var (items, total) = await _carRepository.GetFilteredAsync(filter with { IncludePending = includePending });
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<CarDto?> GetByIdAsync(int id, bool includePending = true)
    {
        var car = await _carRepository.GetByIdAsync(id, includeImages: true);
        if (car is null || (car.Status == CarStatus.ChoDuyet && !includePending)) return null;
        return ToDto(car);
    }

    private async Task EnsureBranchAsync(int? branchId)
    {
        if (!branchId.HasValue || _pricing is null) return;
        var b = await _pricing.GetBranchAsync(branchId.Value);
        if (b is null || !b.IsActive)
            throw new ValidationAppException("BRANCH_INVALID", "Chi nhánh không tồn tại hoặc đã ngừng hoạt động.");
    }

    public async Task<CarDto> CreateAsync(CreateCarRequest request, bool isAdmin)
    {
        if (!isAdmin && request.SecurityDeposit is > 0)
            throw new ForbiddenException("PRICE_ADMIN_ONLY", "Chỉ quản trị viên được đặt tiền cọc xe.");
        if (!isAdmin && request.PricePerHour.HasValue)
            throw new ForbiddenException("PRICE_ADMIN_ONLY", "Chỉ quản trị viên được đặt giá thuê theo giờ.");
        await EnsureBranchAsync(request.BranchId);
        if (await _carRepository.GetByLicensePlateAsync(request.LicensePlate) is not null)
            throw new ConflictException("PLATE_EXISTS", $"Biển số '{request.LicensePlate}' đã tồn tại."); // PLATE_EXISTS

        var car = new CarEntity
        {
            CarType = request.CarType,
            Brand = request.Brand,
            Model = request.Model,
            LicensePlate = request.LicensePlate,
            Seats = request.Seats,
            CarAgeYears = request.CarAgeYears,
            BasePricePerDay = request.BasePricePerDay,
            Description = request.Description,
            Transmission = request.Transmission,
            FuelType = request.FuelType,
            ManufactureYear = request.ManufactureYear,
            SecurityDeposit = request.SecurityDeposit ?? 0,
            BranchId = request.BranchId,
            PricePerHour = request.PricePerHour,
            Status = isAdmin ? CarStatus.SanSang : CarStatus.ChoDuyet
        };

        await _carRepository.AddAsync(car);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(car);
    }

    public async Task UpdateAsync(int id, UpdateCarRequest request, bool isAdmin)
    {
        var car = await _carRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Không tìm thấy xe.");

        if (!isAdmin)
        {
            if (request.BasePricePerDay != car.BasePricePerDay || request.OverridePrice != car.OverridePrice)
                throw new ForbiddenException("PRICE_ADMIN_ONLY", "Chỉ quản trị viên được đổi giá xe.");
            if (request.SecurityDeposit.HasValue && request.SecurityDeposit != car.SecurityDeposit)
                throw new ForbiddenException("PRICE_ADMIN_ONLY", "Chỉ quản trị viên được đổi tiền cọc xe.");
            if (request.PricePerHour.HasValue && request.PricePerHour != car.PricePerHour)
                throw new ForbiddenException("PRICE_ADMIN_ONLY", "Chỉ quản trị viên được đổi giá thuê theo giờ.");
            // Duyệt xe phải qua Admin: Staff không được đưa xe ra/vào trạng thái ChoDuyet.
            if ((request.Status == CarStatus.ChoDuyet) != (car.Status == CarStatus.ChoDuyet))
                throw new ForbiddenException("CAR_APPROVAL_ADMIN_ONLY", "Chỉ quản trị viên được duyệt xe.");
        }

        if (request.BranchId != car.BranchId) await EnsureBranchAsync(request.BranchId);
        var duplicate = await _carRepository.GetByLicensePlateAsync(request.LicensePlate);
        if (duplicate is not null && duplicate.Id != id)
            throw new ConflictException("PLATE_EXISTS", $"Biển số '{request.LicensePlate}' đã tồn tại.");

        car.LicensePlate = request.LicensePlate;
        car.CarType = request.CarType;
        car.Brand = request.Brand;
        car.Model = request.Model;
        car.Seats = request.Seats;
        car.CarAgeYears = request.CarAgeYears;
        car.BasePricePerDay = request.BasePricePerDay;
        car.Status = request.Status;
        car.OverridePrice = request.OverridePrice;
        car.Description = request.Description;
        car.Transmission = request.Transmission;
        car.FuelType = request.FuelType;
        car.ManufactureYear = request.ManufactureYear;
        if (request.SecurityDeposit.HasValue) car.SecurityDeposit = request.SecurityDeposit.Value;
        if (request.BranchId.HasValue) car.BranchId = request.BranchId;
        if (request.PricePerHour.HasValue) car.PricePerHour = request.PricePerHour;

        _carRepository.Update(car);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<CarDto> ApproveAsync(int id, ApproveCarRequest? request)
    {
        var car = await _carRepository.GetByIdAsync(id, includeImages: true)
            ?? throw new NotFoundException("Không tìm thấy xe.");
        if (car.Status != CarStatus.ChoDuyet)
            throw new ConflictException("CAR_NOT_PENDING", "Xe không ở trạng thái chờ duyệt.");
        if (request?.BasePricePerDay is <= 0 || request?.OverridePrice is <= 0)
            throw new BadRequestException("INVALID_PRICE", "Giá phải lớn hơn 0.");

        if (request?.BasePricePerDay is { } basePrice) car.BasePricePerDay = basePrice;
        if (request?.OverridePrice is { } overridePrice) car.OverridePrice = overridePrice;
        car.Status = CarStatus.SanSang;

        _carRepository.Update(car);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(car);
    }

    public async Task DeleteAsync(int id)
    {
        var car = await _carRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Không tìm thấy xe.");

        if (car.Status == CarStatus.DangThue)
            throw new ConflictException("CAR_IN_USE", "Không thể xoá xe đang được thuê.");

        _carRepository.Delete(car);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<CarImageDto> AddImageAsync(int carId, IFormFile file)
    {
        var car = await _carRepository.GetByIdAsync(carId, includeImages: true)
            ?? throw new NotFoundException("Không tìm thấy xe.");

        var relativePath = await _fileStorageService.SaveAsync(file, "cars");
        var image = new CarImageEntity { CarId = carId, ImageUrl = relativePath, IsPrimary = !car.Images.Any() };

        car.Images.Add(image);
        _carRepository.Update(car);
        await _unitOfWork.SaveChangesAsync();

        return new CarImageDto(image.Id, image.ImageUrl, image.IsPrimary);
    }

    public async Task SetPrimaryImageAsync(int carId, int imageId)
    {
        var car = await _carRepository.GetByIdAsync(carId, includeImages: true)
            ?? throw new NotFoundException("Không tìm thấy xe.");

        var target = car.Images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new NotFoundException("Không tìm thấy ảnh.");

        foreach (var image in car.Images)
            image.IsPrimary = image.Id == imageId;

        _carRepository.Update(car);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteImageAsync(int carId, int imageId)
    {
        var car = await _carRepository.GetByIdAsync(carId, includeImages: true)
            ?? throw new NotFoundException("Không tìm thấy xe.");

        var target = car.Images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new NotFoundException("Không tìm thấy ảnh.");

        car.Images.Remove(target);

        // Xóa ảnh đại diện thì chuyển vai trò đại diện sang ảnh còn lại đầu tiên.
        if (target.IsPrimary)
        {
            var next = car.Images.OrderBy(i => i.Id).FirstOrDefault();
            if (next != null) next.IsPrimary = true;
        }

        _carRepository.Update(car);
        await _unitOfWork.SaveChangesAsync();

        _fileStorageService.Delete(target.ImageUrl);
    }

    private static CarDto ToDto(CarEntity car) => new(
        car.Id, car.CarType, car.Brand, car.Model, car.LicensePlate, car.Seats, car.CarAgeYears,
        car.BasePricePerDay, car.Status, car.OverridePrice, car.Description,
        car.Images.Select(i => new CarImageDto(i.Id, i.ImageUrl, i.IsPrimary)).ToList(),
        car.Transmission, car.FuelType, car.ManufactureYear, car.SecurityDeposit, car.BranchId, car.PricePerHour);
}