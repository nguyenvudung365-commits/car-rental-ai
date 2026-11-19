using CarRental.Application.Common;
using CarRental.Application.Modules.Cars;
using CarRental.Application.Modules.Pricing;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;

namespace CarRental.Application.Modules.Bookings;

public partial class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ICarRepository _carRepository;
    private readonly IAiPricingClient _aiPricingClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBookingChargeCalculator _charges;

    public BookingService(
        IBookingRepository bookingRepository,
        ICarRepository carRepository,
        IAiPricingClient aiPricingClient,
        IUnitOfWork unitOfWork,
        IBookingChargeCalculator? charges = null)
    {
        _charges = charges ?? NoCharges.Instance;
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

        var mode = request.Mode ?? RentalMode.TheoNgay;
        ValidateMode(mode, request.StartDate, request.EndDate, car);
        var correlationId = Guid.NewGuid().ToString();
        var rentalDays = mode == RentalMode.TheoGio ? 1 : (request.EndDate - request.StartDate).Days;

        // Gọi AI ngoài transaction để không giữ khóa DB trong lúc chờ timeout/retry.
        var (aiResponse, finalPrice) = await QuoteAsync(car, mode, request.StartDate, rentalDays, correlationId);
        var options = request.ToOptions();
        var rentalAmount = RentalAmount(car, mode, request.StartDate, request.EndDate, finalPrice, rentalDays);
        var lines = await _charges.CalculateAsync(new ChargeContext(customerId, car, request.StartDate, request.EndDate, mode, rentalAmount, options));
        var (pickupBranch, returnBranch) = await _charges.ResolveBranchesAsync(car.BranchId, options);

        // Khóa dòng xe rồi kiểm tra trùng lịch và ghi booking trong cùng một transaction:
        // hai request đồng thời cùng xe phải xếp hàng nên không thể cùng vượt qua bước kiểm tra.
        var bookingId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _carRepository.LockForBookingAsync(request.CarId);

            var hasOverlap = await _bookingRepository.HasOverlapAsync(
                request.CarId,
                request.StartDate,
                request.EndDate);

            if (hasOverlap)
            {
                throw new ConflictException("BOOKING_OVERLAP", "Xe đã có lịch thuê trong khoảng thời gian này");
            }

            var charges = await _charges.RedeemAsync(customerId, lines);
            var booking = new Booking
            {
                CustomerId = customerId,
                CarId = request.CarId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PredictedPricePerDay = aiResponse.PredictedPrice,
                FinalPricePerDay = finalPrice,
                TotalPrice = rentalAmount + charges.Sum(c => c.Amount),
                IsFallback = aiResponse.IsFallback,
                Status = BookingStatus.ChoXacNhan,
                CorrelationId = correlationId,
                Note = request.Note,
                Mode = mode,
                WithDriver = options.WithDriver,
                DeliveryAddress = Trim(options.DeliveryAddress),
                ReturnAddress = Trim(options.ReturnAddress),
                PickupBranchId = pickupBranch,
                ReturnBranchId = returnBranch,
                Charges = charges,
                CreatedAt = DateTime.UtcNow
            };

            await _bookingRepository.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return booking.Id;
        });

        return await GetByIdAsync(bookingId);
    }

    private async Task<(PredictPriceResult Ai, decimal FinalPricePerDay)> QuoteAsync(Car car, RentalMode mode, DateTime start, int rentalDays, string? correlationId = null)
    {
        if (mode == RentalMode.TheoGio) // giá theo giờ cố định theo xe, không cần AI
        {
            var hourly = PricingCalculator.ResolveFinalPricePerDay(car.OverridePrice, null, car.BasePricePerDay);
            return (new PredictPriceResult(hourly, false), hourly);
        }

        var aiResponse = await _aiPricingClient.PredictPriceAsync(new PredictPriceRequest(
            (int)car.CarType,
            car.CarAgeYears,
            car.Seats,
            rentalDays,
            car.BasePricePerDay,
            start,
            correlationId));

        var finalPrice = PricingCalculator.ResolveFinalPricePerDay(
            car.OverridePrice,
            aiResponse.PredictedPrice,
            car.BasePricePerDay);

        return (aiResponse, finalPrice);
    }

    public async Task<BookingEstimateDto> EstimateAsync(EstimateBookingRequest request)
    {
        if (request.StartDate >= request.EndDate)
            throw new BadRequestException("StartDate phải trước EndDate");

        var car = await _carRepository.GetByIdAsync(request.CarId)
            ?? throw new NotFoundException("Car", request.CarId);

        var mode = request.Mode ?? RentalMode.TheoNgay;
        ValidateMode(mode, request.StartDate, request.EndDate, car);
        var rentalDays = mode == RentalMode.TheoGio ? 1 : (request.EndDate - request.StartDate).Days;
        var (ai, finalPrice) = await QuoteAsync(car, mode, request.StartDate, rentalDays);
        var rentalAmount = RentalAmount(car, mode, request.StartDate, request.EndDate, finalPrice, rentalDays);
        var lines = await _charges.CalculateAsync(new ChargeContext(null, car, request.StartDate, request.EndDate, mode, rentalAmount, request.ToOptions()));
        return new BookingEstimateDto(
            rentalDays, car.BasePricePerDay, ai.PredictedPrice, finalPrice, rentalAmount + lines.Sum(l => l.Amount), ai.IsFallback,
            rentalAmount, lines.Select(l => new ChargeLineDto(l.Kind.ToString(), l.Name, l.Amount)).ToList());
    }

    public async Task<List<BookedRangeDto>> GetBookedRangesAsync(int carId)
    {
        var ranges = await _bookingRepository.GetActiveRangesAsync(carId, DateTime.UtcNow.Date);
        return ranges.Select(r => new BookedRangeDto(r.Start, r.End)).ToList();
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

        if (booking.Status is BookingStatus.DaHuy or BookingStatus.DaTraXe or BookingStatus.DangThue)
        {
            throw new ConflictException("INVALID_STATUS", "Không thể hủy booking đã hủy, đang thuê hoặc hoàn thành");
        }

        booking.Status = BookingStatus.DaHuy;
        if (!string.IsNullOrEmpty(reason))
        {
            booking.Note = booking.Note == null
                ? $"Lý do hủy: {reason}"
                : $"{booking.Note}\nLý do hủy: {reason}";
        }

        _bookingRepository.Update(booking);
        await SaveCancelledAsync(booking);

        return ToDto(booking);
    }

    private BookingDto ToDto(Booking booking)
    {
        var rentalDays = booking.Mode == RentalMode.TheoGio ? 1 : (booking.EndDate - booking.StartDate).Days;
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
            booking.CreatedAt,
            booking.Mode,
            booking.WithDriver,
            booking.DeliveryAddress,
            booking.ReturnAddress,
            booking.PickupBranchId,
            booking.ReturnBranchId,
            booking.Charges.Select(c => new ChargeLineDto(c.Kind.ToString(), c.Name, c.Amount)).ToList()
        );
    }
}
