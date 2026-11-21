namespace CarRental.Application.Modules.Bookings;

/// <summary>Gia hạn thuê: khách (hoặc Staff/Admin thay mặt) gửi yêu cầu, Staff/Admin duyệt. customerId khác null nghĩa là người gọi là khách.</summary>
public interface IBookingExtensionService
{
    Task<ExtensionQuoteDto> CheckAsync(int bookingId, DateTime newEndDate, int? customerId = null);
    Task<ExtensionDto> RequestAsync(int bookingId, ExtendBookingRequest request, int requestedById, int? customerId = null);
    Task<List<ExtensionDto>> ListAsync(int bookingId, int? customerId = null);
    Task<List<ExtensionDto>> ListPendingAsync();
    Task<ExtensionDto> ApproveAsync(int extensionId, int reviewerId);
    Task<ExtensionDto> RejectAsync(int extensionId, RejectExtensionRequest request, int reviewerId);
}
