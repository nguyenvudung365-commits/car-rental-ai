using CarRental.Application.Common;
using CarRental.Application.Modules.Cars;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;

namespace CarRental.Application.Modules.Bookings;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ICarRepository _carRepository;
    private readonly IAiPricingClient _aiPricingClient;
    private readonly IUnitOfWork _unitOfWork;

    public BookingService(
        IBookingRepository bookingRepository,
        ICarRepository carRepository,
        IAiPricingClient aiPricingClient,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _carRepository = carRepository;
        _aiPricingClient = aiPricingClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<BookingDto> CreateAsync(int customerId, CreateBookingRequest request)
    {
        if (request.StartDate >= request.EndDate)
        {
            throw new BadRequestException("StartDate phải trước EndDate");
        }

        if (request.StartDate < DateTime.UtcNow.Date)
        {
            throw new BadRequestException("StartDate không được ở quá khứ");
        }

        var car = await _carRepository.GetByIdAsync(request.CarId);
        if (car == null)
        {
            throw new NotFoundException("Car", request.CarId);
        }

        if (car.Status != CarStatus.SanSang)
        {
            throw new ConflictException("CAR_NOT_AVAILABLE", "Xe không sẵn sàng cho thuê");
        }

        var hasOverlap = await _bookingRepository.HasOverlapAsync(
            request.CarId,
            request.StartDate,
            request.EndDate);

        if (hasOverlap)
        {
            throw new ConflictException("BOOKING_OVERLAP", "Xe đã có lịch thuê trong khoảng thời gian này");
        }

        var correlationId = Guid.NewGuid().ToString();
        var rentalDays = (request.EndDate - request.StartDate).Days;

        var aiRequest = new PredictPriceRequest(
            car.CarType.ToString(),
            car.CarAgeYears,
            car.Seats,
            rentalDays,
            car.BasePricePerDay,
            request.StartDate);

        var aiResponse = await _aiPricingClient.PredictPriceAsync(aiRequest);

        var finalPrice = PricingCalculator.ResolveFinalPricePerDay(
            car.OverridePrice,
            aiResponse.PredictedPrice,
            car.BasePricePerDay);

        var booking = new Booking
        {
            CustomerId = customerId,
            CarId = request.CarId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PredictedPricePerDay = aiResponse.PredictedPrice,
            FinalPricePerDay = finalPrice,
            TotalPrice = finalPrice * rentalDays,
            IsFallback = aiResponse.IsFallback,
            Status = BookingStatus.ChoXacNhan,
            CorrelationId = correlationId,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        await _bookingRepository.AddAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(booking.Id);
    }

    public async Task<(List<BookingDto> Items, int TotalCount)> GetFilteredAsync(
        BookingFilterRequest filter,
        int? customerId = null)
    {
        var (items, totalCount) = await _bookingRepository.GetFilteredAsync(filter, customerId);
        var dtos = items.Select(ToDto).ToList();
        return (dtos, totalCount);
    }

    public async Task<BookingDto> GetByIdAsync(int id, int? customerId = null)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, includeDetails: true);
        if (booking == null)
        {
            throw new NotFoundException("Booking", id);
        }

        if (customerId.HasValue && booking.CustomerId != customerId.Value)
        {
            throw new ForbiddenException("Không có quyền xem booking này");
        }

        return ToDto(booking);
    }

    public async Task<BookingDto> ConfirmAsync(int id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, includeDetails: true);
        if (booking == null)
        {
            throw new NotFoundException("Booking", id);
        }

        if (booking.Status != BookingStatus.ChoXacNhan)
        {
            throw new ConflictException("INVALID_STATUS", "Chỉ có thể xác nhận booking đang chờ");
        }

        booking.Status = BookingStatus.DaXacNhan;
        _bookingRepository.Update(booking);
        await _unitOfWork.SaveChangesAsync();

        return ToDto(booking);
    }

    public async Task<BookingDto> CancelAsync(int id, string? reason)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, includeDetails: true);
        if (booking == null)
        {
            throw new NotFoundException("Booking", id);
        }

        if (booking.Status == BookingStatus.DaHuy || booking.Status == BookingStatus.DaTraXe)
        {
            throw new ConflictException("INVALID_STATUS", "Không thể hủy booking đã hủy hoặc hoàn thành");
        }

        booking.Status = BookingStatus.DaHuy;
        if (!string.IsNullOrEmpty(reason))
        {
            booking.Note = booking.Note == null
                ? $"Lý do hủy: {reason}"
                : $"{booking.Note}\nLý do hủy: {reason}";
        }

        _bookingRepository.Update(booking);
        await _unitOfWork.SaveChangesAsync();

        return ToDto(booking);
    }

    private BookingDto ToDto(Booking booking)
    {
        var rentalDays = (booking.EndDate - booking.StartDate).Days;
        var carName = booking.Car != null
            ? $"{booking.Car.Brand} {booking.Car.Model}"
            : "";
        return new BookingDto(
            booking.Id,
            booking.CustomerId,
            booking.Customer?.FullName ?? "",
            booking.CarId,
            carName,
            booking.Car?.LicensePlate ?? "",
            booking.StartDate,
            booking.EndDate,
            rentalDays,
            booking.PredictedPricePerDay,
            booking.FinalPricePerDay,
            booking.TotalPrice,
            booking.IsFallback,
            booking.Status.ToString(),
            booking.CorrelationId,
            booking.Note,
            booking.CreatedAt
        );
    }
}
