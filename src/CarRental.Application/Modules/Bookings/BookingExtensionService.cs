using CarRental.Application.Common;
using CarRental.Application.Modules.Cars;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;

namespace CarRental.Application.Modules.Bookings;

public class BookingExtensionService : IBookingExtensionService
{
    public const int MaxExtraDays = 30;

    private readonly IBookingExtensionRepository _extensions;
    private readonly IBookingRepository _bookings;
    private readonly ICarRepository _cars;
    private readonly IUnitOfWork _unitOfWork;

    public BookingExtensionService(IBookingExtensionRepository extensions, IBookingRepository bookings, ICarRepository cars, IUnitOfWork unitOfWork)
    {
        _extensions = extensions;
        _bookings = bookings;
        _cars = cars;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExtensionQuoteDto> CheckAsync(int bookingId, DateTime newEndDate, int? customerId = null)
    {
        var booking = await LoadExtendableAsync(bookingId, customerId);
        var (days, amount) = Quote(booking, newEndDate);
        var available = !await _bookings.HasOverlapAsync(booking.CarId, booking.EndDate, newEndDate, booking.Id);
        return new ExtensionQuoteDto(booking.EndDate, newEndDate, days, amount, booking.TotalPrice + amount, available);
    }

    public async Task<ExtensionDto> RequestAsync(int bookingId, ExtendBookingRequest request, int requestedById, int? customerId = null)
    {
        var booking = await LoadExtendableAsync(bookingId, customerId);
        var (_, amount) = Quote(booking, request.NewEndDate);

        if (await _extensions.HasPendingAsync(bookingId)) throw Pending();
        if (await _bookings.HasOverlapAsync(booking.CarId, booking.EndDate, request.NewEndDate, booking.Id))
            throw new ConflictException("EXTENSION_UNAVAILABLE", "Xe đã có lịch thuê khác trong thời gian gia hạn.");

        var ext = new BookingExtension
        {
            BookingId = bookingId, OldEndDate = booking.EndDate, NewEndDate = request.NewEndDate,
            ExtraAmount = amount, RequestedById = requestedById
        };
        await _extensions.AddAsync(ext);
        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Unique index chỉ cho một yêu cầu chờ duyệt mỗi đơn; lỗi khác ném lại nguyên.
            if (await _extensions.HasPendingAsync(bookingId)) throw Pending();
            throw;
        }
        return ToDto(ext, booking);
    }

    public async Task<List<ExtensionDto>> ListAsync(int bookingId, int? customerId = null)
    {
        var booking = await LoadAsync(bookingId, customerId);
        return (await _extensions.ListByBookingAsync(bookingId)).Select(e => ToDto(e, booking)).ToList();
    }

    public async Task<List<ExtensionDto>> ListPendingAsync()
        => (await _extensions.ListByStatusAsync(ExtensionStatus.ChoDuyet)).Select(e => ToDto(e, e.Booking)).ToList();

    public async Task<ExtensionDto> ApproveAsync(int extensionId, int reviewerId)
        => await ReviewAsync(extensionId, reviewerId, approve: true, reason: null);

    public async Task<ExtensionDto> RejectAsync(int extensionId, RejectExtensionRequest request, int reviewerId)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BadRequestException("REASON_REQUIRED", "Cần nhập lý do từ chối.");
        return await ReviewAsync(extensionId, reviewerId, approve: false, reason: request.Reason.Trim());
    }

    /// <summary>Duyệt/từ chối cùng một khuôn: khóa xe, rồi mới nạp entity (có tracking) và kiểm lại trạng thái, nên hai thao tác song song chỉ một thành công.</summary>
    private async Task<ExtensionDto> ReviewAsync(int extensionId, int reviewerId, bool approve, string? reason)
    {
        var carId = await _extensions.GetCarIdAsync(extensionId) ?? throw new NotFoundException("Yêu cầu gia hạn", extensionId);
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _cars.LockForBookingAsync(carId);
            var ext = await LoadPendingAsync(extensionId);
            var booking = ext.Booking!;
            if (approve)
            {
                if (booking.Status is not (BookingStatus.DaXacNhan or BookingStatus.DangThue) || booking.EndDate != ext.OldEndDate)
                    throw new ConflictException("EXTENSION_STALE", "Đơn đã thay đổi, không thể duyệt yêu cầu gia hạn này.");
                if (await _bookings.HasOverlapAsync(booking.CarId, booking.EndDate, ext.NewEndDate, booking.Id))
                    throw new ConflictException("EXTENSION_UNAVAILABLE", "Xe đã có lịch thuê khác trong thời gian gia hạn.");
                booking.EndDate = ext.NewEndDate;
                booking.TotalPrice += ext.ExtraAmount;
                booking.UpdatedAt = DateTime.UtcNow;
            }
            else ext.RejectReason = reason;
            Stamp(ext, approve ? ExtensionStatus.DaDuyet : ExtensionStatus.TuChoi, reviewerId);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(ext, booking);
        });
    }

    /// <summary>Phụ thu = số ngày thêm × giá chốt/ngày của đơn (giữ giá đã chốt, không gọi lại AI), qua PricingCalculator.</summary>
    private static (int Days, decimal Amount) Quote(Booking booking, DateTime newEndDate)
    {
        if (newEndDate <= booking.EndDate)
            throw new BadRequestException("INVALID_NEW_END_DATE", "Ngày trả mới phải sau ngày trả hiện tại.");
        // Cùng công thức với BookingService.CreateAsync: số ngày nguyên (cắt phần lẻ), nên lệch dưới 24 giờ bị từ chối.
        var days = (newEndDate - booking.EndDate).Days;
        if (days < 1) throw new BadRequestException("INVALID_NEW_END_DATE", "Cần gia hạn ít nhất 1 ngày.");
        if (days > MaxExtraDays)
            throw new BadRequestException("EXTENSION_TOO_LONG", $"Mỗi lần gia hạn tối đa {MaxExtraDays} ngày.");
        return (days, PricingCalculator.ExtensionCharge(booking.FinalPricePerDay, days));
    }

    private async Task<Booking> LoadAsync(int bookingId, int? customerId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, includeDetails: true)
            ?? throw new NotFoundException("Booking", bookingId);
        if (customerId.HasValue && booking.CustomerId != customerId.Value)
            throw new ForbiddenException("Không có quyền thao tác booking này");
        return booking;
    }

    private async Task<Booking> LoadExtendableAsync(int bookingId, int? customerId)
    {
        var booking = await LoadAsync(bookingId, customerId);
        if (booking.Status is not (BookingStatus.DaXacNhan or BookingStatus.DangThue))
            throw new ConflictException("INVALID_STATUS", "Chỉ gia hạn booking đã xác nhận hoặc đang thuê.");
        if (booking.Mode == RentalMode.TheoGio)
            throw new ConflictException("HOURLY_NOT_EXTENDABLE", "Đơn thuê theo giờ không gia hạn được, hãy đặt đơn mới.");
        return booking;
    }

    private async Task<BookingExtension> LoadPendingAsync(int id)
    {
        var ext = await _extensions.GetByIdAsync(id) ?? throw new NotFoundException("Yêu cầu gia hạn", id);
        if (ext.Status != ExtensionStatus.ChoDuyet)
            throw new ConflictException("INVALID_STATUS", "Yêu cầu gia hạn đã được xử lý.");
        return ext;
    }

    private static void Stamp(BookingExtension ext, ExtensionStatus status, int reviewerId)
    {
        ext.Status = status;
        ext.ReviewedById = reviewerId;
        ext.ReviewedAt = DateTime.UtcNow;
        ext.UpdatedAt = DateTime.UtcNow;
    }

    private static ConflictException Pending()
        => new("EXTENSION_PENDING", "Booking đã có yêu cầu gia hạn đang chờ duyệt.");

    private static ExtensionDto ToDto(BookingExtension e, Booking? b) => new(
        e.Id, e.BookingId, e.OldEndDate, e.NewEndDate, (e.NewEndDate - e.OldEndDate).Days, e.ExtraAmount, e.Status,
        e.RejectReason, e.RequestedById, e.CreatedAt, e.ReviewedAt, b?.Customer?.FullName,
        b?.Car is null ? null : $"{b.Car.Brand} {b.Car.Model}");
}
