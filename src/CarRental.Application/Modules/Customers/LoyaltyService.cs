using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;

namespace CarRental.Application.Modules.Customers;

public class LoyaltyService : ILoyaltyService
{
    private const int HistorySize = 20;

    private readonly ICustomerRepository _customers;
    private readonly ILoyaltyRepository _loyalty;
    private readonly IUnitOfWork _unitOfWork;

    public LoyaltyService(ICustomerRepository customers, ILoyaltyRepository loyalty, IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _loyalty = loyalty;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoyaltyDto> GetAsync(int customerId)
    {
        var customer = await _customers.GetByIdAsync(customerId)
            ?? throw new NotFoundException("Không tìm thấy khách hàng.");
        var history = await _loyalty.GetRecentAsync(customerId, HistorySize);
        var next = LoyaltyPolicy.NextTier(customer.LifetimePoints);
        return new LoyaltyDto(
            customer.LoyaltyPoints, customer.LifetimePoints, LoyaltyPolicy.TierFor(customer.LifetimePoints),
            next?.Next, next?.PointsToNext ?? 0,
            history.Select(h => new LoyaltyTransactionDto(h.Id, h.BookingId, h.Points, h.Reason, h.Note, h.CreatedAt)).ToList());
    }

    // LoyaltyPoints/LifetimePoints là concurrency token: ghi đồng thời thì một bên lỗi, nạp lại số mới và thử lại.
    private const int MaxAttempts = 5;

    public async Task AwardForBookingAsync(int customerId, int bookingId, decimal orderValue)
    {
        var points = LoyaltyPolicy.PointsFor(orderValue);
        if (points == 0 || await _loyalty.ExistsAsync(bookingId, LoyaltyReason.TichDiem)) return;

        for (var attempt = 1; ; attempt++)
        {
            var customer = await _customers.GetByIdAsync(customerId)
                ?? throw new NotFoundException("Không tìm thấy khách hàng.");
            customer.LoyaltyPoints += points;
            customer.LifetimePoints += points;
            await _loyalty.AddAsync(new LoyaltyTransaction
            {
                CustomerId = customerId, BookingId = bookingId, Points = points, Reason = LoyaltyReason.TichDiem,
                Note = $"Đơn #{bookingId}"
            });
            try
            {
                await _unitOfWork.SaveChangesAsync();
                return;
            }
            // Application không tham chiếu EF: bắt mọi lỗi lưu. Giao dịch đã tồn tại (request song song thắng) thì coi như đã cộng.
            catch (Exception)
            {
                if (await _loyalty.ExistsAsync(bookingId, LoyaltyReason.TichDiem)) return;
                if (attempt >= MaxAttempts) throw;
                await _loyalty.ResetTrackingAsync();
            }
        }
    }

    public async Task<LoyaltyDto> AdjustAsync(int customerId, AdjustLoyaltyRequest request)
    {
        if (request.Points == 0)
            throw new BadRequestException("INVALID_POINTS", "Số điểm điều chỉnh phải khác 0.");
        if (string.IsNullOrWhiteSpace(request.Note))
            throw new BadRequestException("REASON_REQUIRED", "Cần nhập lý do điều chỉnh.");

        for (var attempt = 1; ; attempt++)
        {
            var customer = await _customers.GetByIdAsync(customerId)
                ?? throw new NotFoundException("Không tìm thấy khách hàng.");
            if (customer.LoyaltyPoints + request.Points < 0)
                throw new BadRequestException("INSUFFICIENT_POINTS", "Số dư điểm không đủ để trừ.");

            customer.LoyaltyPoints += request.Points;
            if (request.Points > 0) customer.LifetimePoints += request.Points; // hạng không bị trừ khi điều chỉnh giảm
            await _loyalty.AddAsync(new LoyaltyTransaction
            {
                CustomerId = customerId, Points = request.Points, Reason = LoyaltyReason.DieuChinh, Note = request.Note.Trim()
            });
            try
            {
                await _unitOfWork.SaveChangesAsync();
                break;
            }
            catch (Exception)
            {
                if (attempt >= MaxAttempts) throw;
                await _loyalty.ResetTrackingAsync();
            }
        }
        return await GetAsync(customerId);
    }
}
