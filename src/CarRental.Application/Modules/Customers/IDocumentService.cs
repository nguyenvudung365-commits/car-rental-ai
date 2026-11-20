namespace CarRental.Application.Modules.Customers;

/// <summary>Cửa để module Bookings hỏi giấy tờ của khách đã đủ điều kiện nhận xe chưa.</summary>
public interface IDocumentVerification
{
    /// <summary>CCCD đã duyệt và bằng lái đã duyệt còn hạn tới hết ngày endDate.</summary>
    Task<bool> IsVerifiedForRentalAsync(int customerId, DateTime endDate);
}

public interface IDocumentService : IDocumentVerification
{
    Task<List<DocumentDto>> GetMineAsync(int customerId);
    Task<DocumentDto> SubmitAsync(int customerId, SubmitDocumentRequest request);
    Task<(List<DocumentDto> Items, int TotalCount)> ListQueueAsync(DocumentFilterRequest filter);
    Task<DocumentDto> ApproveAsync(int id);
    Task<DocumentDto> RejectAsync(int id, RejectDocumentRequest request);
    /// <summary>Ảnh giấy tờ (side = front|back): chỉ chủ sở hữu hoặc Staff/Admin.</summary>
    Task<DocumentFile> GetFileAsync(int id, string side);
}
