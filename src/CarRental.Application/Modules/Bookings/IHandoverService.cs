using Microsoft.AspNetCore.Http;

namespace CarRental.Application.Modules.Bookings;

/// <summary>Biên bản giao/nhận xe. customerId khác null nghĩa là người gọi là khách (chỉ xem đơn của mình).</summary>
public interface IHandoverService
{
    Task<HandoverDto> CreateAsync(int bookingId, CreateHandoverRequest request, int? staffId);
    Task<HandoverPhotoDto> AddPhotoAsync(int handoverId, IFormFile file);
    Task<List<HandoverDto>> ListAsync(int bookingId, int? customerId = null);
    Task<HandoverPhotoFile> GetPhotoAsync(int photoId, int? customerId = null);
}
