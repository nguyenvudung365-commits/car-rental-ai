using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Customers;

/// <summary>Lịch sử điểm thưởng. Unique (BookingId, Reason) khi có BookingId để không cộng trùng.</summary>
public class LoyaltyTransaction : BaseEntity
{
    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int? BookingId { get; set; }

    /// <summary>Số điểm thay đổi (âm khi Admin điều chỉnh giảm).</summary>
    public int Points { get; set; }

    public LoyaltyReason Reason { get; set; }

    public string? Note { get; set; }
}
