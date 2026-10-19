using CarRental.Application.Common;
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

    public CarService(ICarRepository carRepository, IFileStorageService fileStorageService, IUnitOfWork unitOfWork)
    {
        _carRepository = carRepository;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<(List<CarDto> Items, int TotalCount)> GetFilteredAsync(CarFilterRequest filter)
    {
        var (items, total) = await _carRepository.GetFilteredAsync(filter);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<CarDto?> GetByIdAsync(int id)
    {
        var car = await _carRepository.GetByIdAsync(id, includeImages: true);
        return car is null ? null : ToDto(car);
    }

    public async Task<CarDto> CreateAsync(CreateCarRequest request)
    {
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
            Status = CarStatus.SanSang
        };

        await _carRepository.AddAsync(car);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(car);
    }

    public async Task UpdateAsync(int id, UpdateCarRequest request)
    {
        var car = await _carRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Không tìm thấy xe.");

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

        _carRepository.Update(car);
        await _unitOfWork.SaveChangesAsync();
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

    private static CarDto ToDto(CarEntity car) => new(
        car.Id, car.CarType, car.Brand, car.Model, car.LicensePlate, car.Seats, car.CarAgeYears,
        car.BasePricePerDay, car.Status, car.OverridePrice, car.Description,
        car.Images.Select(i => new CarImageDto(i.Id, i.ImageUrl, i.IsPrimary)).ToList());
}