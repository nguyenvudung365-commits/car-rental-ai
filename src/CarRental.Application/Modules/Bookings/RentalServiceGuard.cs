using CarRental.Application.Common;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using Microsoft.Extensions.Logging;

namespace CarRental.Application.Modules.Bookings;

/// <summary>Decorator quanh RentalService: chặn giao xe khi giấy tờ chưa duyệt, thiếu biên bản giao/cọc; chặn trả xe khi thiếu biên bản nhận; cộng điểm thưởng sau khi trả xe; còn lại chuyển tiếp.</summary>
public class RentalServiceGuard : IRentalService
{
    private readonly IRentalService _inner;
    private readonly IBookingService _bookings;
    private readonly IDocumentVerification _documents;
    private readonly ILoyaltyService _loyalty;
    private readonly IHandoverRepository? _handovers;
    private readonly IDepositService? _deposits;
    private readonly ILogger<RentalServiceGuard>? _log;

    // Tham số cuối là tùy chọn: null thì bỏ qua luật tương ứng (test cũ khởi tạo bằng 4 tham số).
    public RentalServiceGuard(IRentalService inner, IBookingService bookings, IDocumentVerification documents, ILoyaltyService loyalty,
        IHandoverRepository? handovers = null, IDepositService? deposits = null, ILogger<RentalServiceGuard>? log = null)
    {
        _handovers = handovers;
        _deposits = deposits;
        _log = log;
        _inner = inner;
        _bookings = bookings;
        _documents = documents;
        _loyalty = loyalty;
    }

    public Task<PaymentDto> CreatePaymentAsync(int bookingId, CreatePaymentRequest request, int? customerId = null)
    {
        // TruCoc chỉ do quyết toán cọc tạo ra, không cho tạo tay.
        if (request.Method == PaymentMethod.TruCoc)
            throw new BadRequestException("INVALID_METHOD", "Phương thức TruCoc chỉ được tạo khi quyết toán cọc.");
        return _inner.CreatePaymentAsync(bookingId, request, customerId);
    }

    public Task<BookingPaymentsDto> GetPaymentsAsync(int bookingId, int? customerId = null)
        => _inner.GetPaymentsAsync(bookingId, customerId);

    public Task<PaymentDto> MarkPaymentPaidAsync(int paymentId, MarkPaymentPaidRequest request)
        => _inner.MarkPaymentPaidAsync(paymentId, request);

    public async Task<BookingDto> PickupAsync(int bookingId)
    {
        // Không chặn lúc đặt xe; chỉ chặn lúc giao. Trạng thái sai thì để service gốc báo INVALID_STATUS.
        var booking = await _bookings.GetByIdAsync(bookingId);
        if (booking.Status == nameof(CarRental.Domain.Common.BookingStatus.DaXacNhan)
            && !await _documents.IsVerifiedForRentalAsync(booking.CustomerId, booking.EndDate))
            throw new ConflictException("DOCUMENTS_NOT_VERIFIED",
                "Khách chưa có CCCD và bằng lái được duyệt (bằng lái phải còn hạn đến ngày trả xe).");
        if (booking.Status == nameof(BookingStatus.DaXacNhan))
        {
            if (_handovers is not null && !await _handovers.ExistsAsync(bookingId, HandoverType.Giao))
                throw new ConflictException("HANDOVER_REQUIRED", "Cần lập biên bản giao xe trước khi giao.");
            if (_deposits is not null && !await _deposits.IsSatisfiedForPickupAsync(bookingId))
                throw new ConflictException("DEPOSIT_REQUIRED", "Cần thu tiền cọc trước khi giao xe.");
        }
        return await _inner.PickupAsync(bookingId);
    }

    public async Task<ReturnRecordDto> ReturnAsync(int bookingId, ReturnBookingRequest request, int? staffId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId);
        if (booking.Status == nameof(BookingStatus.DangThue)
            && _handovers is not null && !await _handovers.ExistsAsync(bookingId, HandoverType.Nhan))
            throw new ConflictException("HANDOVER_REQUIRED", "Cần lập biên bản nhận xe trước khi ghi nhận trả xe.");
        var record = await _inner.ReturnAsync(bookingId, request, staffId);
        // Hai lần lưu riêng: lỗi cộng điểm không làm hỏng việc trả xe đã lưu; ghi log để Admin điều chỉnh tay (cộng điểm idempotent theo đơn).
        try
        {
            await _loyalty.AwardForBookingAsync(booking.CustomerId, bookingId, record.FinalAmount);
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "Không cộng được điểm thưởng cho đơn {BookingId} sau khi trả xe", bookingId);
        }
        return record;
    }

    public Task<ReturnRecordDto> GetReturnAsync(int bookingId, int? customerId = null)
        => _inner.GetReturnAsync(bookingId, customerId);

    public Task<ContractFile> GetContractAsync(int bookingId, int? customerId = null)
        => _inner.GetContractAsync(bookingId, customerId);
}
