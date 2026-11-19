namespace CarRental.Application.Modules.Customers;

/// <summary>Truy vấn tổng hợp cho trang quản lý khách (cài đặt ở Infrastructure để không kéo module Bookings vào Customers).</summary>
public interface ICustomerAdminRepository
{
    Task<(List<CustomerListRow> Items, int TotalCount)> SearchAsync(CustomerListFilterRequest filter, int page, int pageSize);
    /// <summary>BookingCount (không tính đơn hủy) và TotalSpent (tổng giá trị các đơn đã trả xe).</summary>
    Task<(int BookingCount, decimal TotalSpent)> GetStatsAsync(int customerId);
    Task<List<CustomerBookingBriefDto>> GetRecentBookingsAsync(int customerId, int take);
}
