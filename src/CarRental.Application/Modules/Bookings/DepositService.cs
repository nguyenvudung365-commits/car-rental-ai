using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;

namespace CarRental.Application.Modules.Bookings;

public class DepositService : IDepositService
{
    private readonly IDepositRepository _deposits;
    private readonly IBookingRepository _bookings;
    private readonly IRentalService _rental;
    private readonly IUnitOfWork _unitOfWork;

    /// <param name="rental">Phải là RentalService gốc (không phải Guard) để Guard phụ thuộc IDepositService mà không vòng.</param>
    public DepositService(IDepositRepository deposits, IBookingRepository bookings, IRentalService rental, IUnitOfWork unitOfWork)
    {
        _deposits = deposits;
        _bookings = bookings;
        _rental = rental;
        _unitOfWork = unitOfWork;
    }

    public async Task<DepositSummaryDto> GetAsync(int bookingId, int? customerId = null)
    {
        var booking = await LoadAsync(bookingId, customerId);
        return Summarize(booking.Car?.SecurityDeposit ?? 0, await _deposits.ListByBookingAsync(bookingId));
    }

    public async Task<bool> IsSatisfiedForPickupAsync(int bookingId)
    {
        var booking = await LoadAsync(bookingId, null);
        if ((booking.Car?.SecurityDeposit ?? 0) <= 0) return true;
        return (await _deposits.ListByBookingAsync(bookingId)).Any(t => t.Type == DepositTxType.Thu);
    }

    public async Task<DepositSummaryDto> CollectAsync(int bookingId, CollectDepositRequest request, int? staffId)
    {
        if (!Enum.IsDefined(request.Method) || request.Method == PaymentMethod.TruCoc)
            throw new BadRequestException("INVALID_METHOD", "Phương thức thu cọc không hợp lệ.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _deposits.LockBookingAsync(bookingId);
            var booking = await LoadAsync(bookingId, null);
            if (booking.Status != BookingStatus.DaXacNhan)
                throw new ConflictException("INVALID_STATUS", "Chỉ thu cọc cho booking đã xác nhận.");
            var required = booking.Car?.SecurityDeposit ?? 0;
            if (required <= 0)
                throw new ConflictException("DEPOSIT_NOT_REQUIRED", "Xe này không yêu cầu đặt cọc.");
            if ((await _deposits.ListByBookingAsync(bookingId)).Any(t => t.Type == DepositTxType.Thu))
                throw new ConflictException("DEPOSIT_COLLECTED", "Booking đã thu cọc.");

            await _deposits.AddAsync(new DepositTransaction
            {
                BookingId = bookingId, Type = DepositTxType.Thu, Amount = required, Method = request.Method, StaffId = staffId
            });
            await _unitOfWork.SaveChangesAsync();
            return 0;
        });
        return await GetAsync(bookingId);
    }

    public async Task<DepositSummaryDto> SettleAsync(int bookingId, SettleDepositRequest request, int? staffId)
    {
        if (!Enum.IsDefined(request.RefundMethod) || request.RefundMethod == PaymentMethod.TruCoc)
            throw new BadRequestException("INVALID_METHOD", "Phương thức hoàn cọc không hợp lệ.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _deposits.LockBookingAsync(bookingId);
            var booking = await LoadAsync(bookingId, null);
            // Đơn đã hủy sau khi thu cọc: hoàn toàn bộ, không khấu trừ.
            if (booking.Status is not (BookingStatus.DaTraXe or BookingStatus.DaHuy))
                throw new ConflictException("INVALID_STATUS", "Chỉ quyết toán cọc sau khi trả xe hoặc khi đơn đã hủy.");

            var txs = await _deposits.ListByBookingAsync(bookingId);
            var held = txs.Where(t => t.Type == DepositTxType.Thu).Sum(t => t.Amount);
            if (held <= 0) throw new ConflictException("DEPOSIT_NOT_COLLECTED", "Booking chưa thu cọc.");
            if (txs.Any(t => t.Type is DepositTxType.KhauTru or DepositTxType.Hoan))
                throw new ConflictException("DEPOSIT_SETTLED", "Cọc của booking đã được quyết toán.");

            var payments = await _rental.GetPaymentsAsync(bookingId);
            // Khoản chờ có thể sắp được ghi nhận: xử lý trước để không khấu trừ cọc rồi thu thừa (đơn hủy không khấu trừ nên bỏ qua).
            if (booking.Status != BookingStatus.DaHuy && booking.Payments.Any(p => !p.IsDeleted && p.Status == PaymentStatus.ChuaThanhToan))
                throw new ConflictException("PAYMENT_PENDING", "Còn khoản thanh toán chưa xử lý, hãy ghi nhận hoặc hủy trước khi quyết toán cọc.");
            var outstanding = booking.Status == BookingStatus.DaHuy ? 0 : payments.AmountDue;
            var deduct = Math.Min(held, outstanding);
            var note = request.Note?.Trim();

            if (deduct > 0)
            {
                await _bookings.AddPaymentAsync(new Payment
                {
                    BookingId = bookingId, Amount = deduct, Method = PaymentMethod.TruCoc, Status = PaymentStatus.DaThanhToan,
                    PaidAt = DateTime.UtcNow, TransactionCode = "TRU-COC"
                });
                await _deposits.AddAsync(new DepositTransaction
                {
                    BookingId = bookingId, Type = DepositTxType.KhauTru, Amount = deduct, Method = PaymentMethod.TruCoc, Note = note, StaffId = staffId
                });
            }
            if (held - deduct > 0)
                await _deposits.AddAsync(new DepositTransaction
                {
                    BookingId = bookingId, Type = DepositTxType.Hoan, Amount = held - deduct, Method = request.RefundMethod, Note = note, StaffId = staffId
                });
            await _unitOfWork.SaveChangesAsync();
            return 0;
        });
        return await GetAsync(bookingId);
    }

    private async Task<Booking> LoadAsync(int bookingId, int? customerId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, includeDetails: true)
            ?? throw new NotFoundException("Booking", bookingId);
        if (customerId.HasValue && booking.CustomerId != customerId.Value)
            throw new ForbiddenException("Không có quyền xem cọc của booking này");
        return booking;
    }

    private static DepositSummaryDto Summarize(decimal required, List<DepositTransaction> txs)
    {
        decimal Sum(DepositTxType t) => txs.Where(x => x.Type == t).Sum(x => x.Amount);
        var collected = Sum(DepositTxType.Thu);
        var deducted = Sum(DepositTxType.KhauTru);
        var refunded = Sum(DepositTxType.Hoan);
        var status = collected == 0 ? "ChuaThu"
            : txs.Any(t => t.Type is DepositTxType.KhauTru or DepositTxType.Hoan) ? "DaQuyetToan" : "DangGiu";
        return new DepositSummaryDto(
            collected > 0 ? collected : required, collected, collected - deducted - refunded, deducted, refunded, status,
            txs.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
                .Select(t => new DepositTransactionDto(t.Id, t.Type.ToString(), t.Amount, t.Method.ToString(), t.Note, t.CreatedAt)).ToList());
    }
}
