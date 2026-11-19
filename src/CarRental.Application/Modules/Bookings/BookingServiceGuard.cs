using CarRental.Application.Common;
using CarRental.Application.Modules.Cars;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Bookings;

/// <summary>Decorator quanh BookingService: chặn khách trong danh sách chặn, các thao tác khác chuyển tiếp.</summary>
public class BookingServiceGuard : IBookingService
{
    private readonly IBookingService _inner;
    private readonly ICustomerScreening _screening;
    private readonly ICarRepository? _cars;
    private readonly ICurrentUser? _me;

    public BookingServiceGuard(IBookingService inner, ICustomerScreening screening, ICarRepository? cars = null, ICurrentUser? me = null)
    {
        _inner = inner;
        _screening = screening;
        _cars = cars;
        _me = me;
    }

    /// <summary>Xe ChoDuyet không công khai: khách gọi estimate/booked-ranges nhận 404 như khi xe không tồn tại.</summary>
    private async Task HideCarPendingApprovalAsync(int carId)
    {
        if (_cars is null || _me?.IsStaffOrAdmin == true) return;
        var car = await _cars.GetByIdAsync(carId);
        if (car?.Status == CarStatus.ChoDuyet) throw new NotFoundException("Car", carId);
    }

    public async Task<BookingDto> CreateAsync(int customerId, CreateBookingRequest request)
    {
        // Thông báo cho khách không lộ lý do chặn. Mức CanhBao vẫn cho đặt.
        if ((await _screening.ScreenAsync(customerId)).Level == BlacklistLevel.Chan)
            throw new ForbiddenException("CUSTOMER_BLOCKED", "Tài khoản không thể đặt xe. Vui lòng liên hệ cửa hàng.");
        return await _inner.CreateAsync(customerId, request);
    }

    public async Task<BookingDto> ConfirmAsync(int id)
    {
        // Chặn cả đơn tạo trước khi khách bị đưa vào danh sách.
        var booking = await _inner.GetByIdAsync(id);
        var screening = await _screening.ScreenAsync(booking.CustomerId);
        if (screening.Level == BlacklistLevel.Chan)
            throw new ConflictException("CUSTOMER_BLOCKED", $"Khách đang bị chặn: {screening.Reason}");
        return await _inner.ConfirmAsync(id);
    }

    public Task<(List<BookingDto> Items, int TotalCount)> GetFilteredAsync(BookingFilterRequest filter, int? customerId = null)
        => _inner.GetFilteredAsync(filter, customerId);

    public Task<BookingDto> GetByIdAsync(int id, int? customerId = null) => _inner.GetByIdAsync(id, customerId);

    public Task<BookingDto> CancelAsync(int id, string? reason) => _inner.CancelAsync(id, reason);

    public async Task<BookingEstimateDto> EstimateAsync(EstimateBookingRequest request)
    {
        await HideCarPendingApprovalAsync(request.CarId);
        return await _inner.EstimateAsync(request);
    }

    public async Task<List<BookedRangeDto>> GetBookedRangesAsync(int carId)
    {
        await HideCarPendingApprovalAsync(carId);
        return await _inner.GetBookedRangesAsync(carId);
    }
}
