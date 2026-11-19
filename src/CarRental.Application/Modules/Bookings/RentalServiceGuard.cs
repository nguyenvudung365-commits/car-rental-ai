using CarRental.Application.Common;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using Microsoft.Extensions.Logging;

namespace CarRental.Application.Modules.Bookings;

/// <summary>Decorator quanh RentalService: chặn giao xe khi giấy tờ chưa duyệt; cộng điểm thưởng sau khi trả xe; còn lại chuyển tiếp.</summary>
public class RentalServiceGuard : IRentalService
{
    private readonly IRentalService _inner;
    private readonly IBookingService _bookings;
    private readonly IDocumentVerification _documents;
    private readonly ILoyaltyService _loyalty;
    private readonly ILogger<RentalServiceGuard>? _log;

    public RentalServiceGuard(IRentalService inner, IBookingService bookings, IDocumentVerification documents, ILoyaltyService loyalty,
        ILogger<RentalServiceGuard>? log = null)
    {
        _log = log;
        _inner = inner;
        _bookings = bookings;
        _documents = documents;
        _loyalty = loyalty;
    }

    public Task<PaymentDto> CreatePaymentAsync(int bookingId, CreatePaymentRequest request, int? customerId = null)
    {
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
        return await _inner.PickupAsync(bookingId);
    }

    public async Task<ReturnRecordDto> ReturnAsync(int bookingId, ReturnBookingRequest request, int? staffId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId);
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
