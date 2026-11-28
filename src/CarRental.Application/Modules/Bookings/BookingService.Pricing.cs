using CarRental.Application.Common;
using CarRental.Application.Modules.Pricing;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;

namespace CarRental.Application.Modules.Bookings;

// Phần tính giá của BookingService (đợt 4): giờ/ngày, phụ phí, mã giảm giá, giao nhận, chi nhánh.
public partial class BookingService
{
    public const int MinHourlyHours = 4;
    public const int MaxHourlyHours = 12;
    public const int MinHourlyLeadHours = 2;

    private static void ValidateMode(RentalMode mode, DateTime start, DateTime end, Car car)
    {
        if (mode != RentalMode.TheoGio)
        {
            if ((end - start).Days < 1)
                throw new BadRequestException("BOOKING_TOO_SHORT", "Thuê theo ngày phải từ 1 ngày (24 giờ) trở lên.");
            return;
        }
        if (car.PricePerHour is null or <= 0)
            throw new BadRequestException("HOURLY_NOT_SUPPORTED", "Xe này không hỗ trợ thuê theo giờ.");
        if (start < DateTime.UtcNow.AddHours(MinHourlyLeadHours))
            throw new BadRequestException("HOURLY_START", $"Thuê theo giờ phải đặt trước ít nhất {MinHourlyLeadHours} giờ.");
        var hours = (end - start).TotalHours;
        if (hours < MinHourlyHours || hours > MaxHourlyHours)
            throw new BadRequestException("HOURLY_RANGE", $"Thuê theo giờ phải từ {MinHourlyHours} đến {MaxHourlyHours} giờ.");
    }

    /// <summary>Tiền thuê chưa gồm phụ phí. Theo giờ: PricePerHour của xe (đã kiểm tra ở ValidateMode).</summary>
    private static decimal RentalAmount(Car car, RentalMode mode, DateTime start, DateTime end, decimal finalPricePerDay, int rentalDays)
    {
        if (mode != RentalMode.TheoGio) return finalPricePerDay * rentalDays;
        var hours = (decimal)Math.Ceiling((end - start).TotalHours);
        return Math.Round(hours * car.PricePerHour!.Value, 0);
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Hủy đơn: lưu và trả lượt mã trong cùng transaction để không lệch UsedCount.</summary>
    private async Task SaveCancelledAsync(Booking booking)
    {
        if (!booking.Charges.Any(c => c.CouponId.HasValue))
        {
            await _unitOfWork.SaveChangesAsync();
            return;
        }
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.SaveChangesAsync();
            await _charges.ReleaseAsync(booking.Charges);
            return 0;
        });
    }
}

public static class BookingOptionsMapping
{
    public static BookingOptions ToOptions(this CreateBookingRequest r) =>
        new(r.CouponCode, r.WithDriver, r.DeliveryAddress, r.ReturnAddress, r.PickupBranchId, r.ReturnBranchId);

    public static BookingOptions ToOptions(this EstimateBookingRequest r) =>
        new(r.CouponCode, r.WithDriver, r.DeliveryAddress, r.ReturnAddress, r.PickupBranchId, r.ReturnBranchId);
}
