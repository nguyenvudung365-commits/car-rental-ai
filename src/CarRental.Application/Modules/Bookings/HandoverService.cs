using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using Microsoft.AspNetCore.Http;

namespace CarRental.Application.Modules.Bookings;

public class HandoverService : IHandoverService
{
    public const int MaxPhotos = 10;

    private readonly IHandoverRepository _handovers;
    private readonly IBookingRepository _bookings;
    private readonly IFileStorageService _files;
    private readonly IUnitOfWork _unitOfWork;

    public HandoverService(IHandoverRepository handovers, IBookingRepository bookings, IFileStorageService files, IUnitOfWork unitOfWork)
    {
        _handovers = handovers;
        _bookings = bookings;
        _files = files;
        _unitOfWork = unitOfWork;
    }

    public async Task<HandoverDto> CreateAsync(int bookingId, CreateHandoverRequest request, int? staffId)
    {
        if (!Enum.IsDefined(request.Type))
            throw new BadRequestException("INVALID_HANDOVER_TYPE", "Loại biên bản không hợp lệ.");
        var booking = await _bookings.GetByIdAsync(bookingId) ?? throw new NotFoundException("Booking", bookingId);

        if (request.Type == HandoverType.Giao)
        {
            if (booking.Status != BookingStatus.DaXacNhan)
                throw new ConflictException("INVALID_STATUS", "Chỉ lập biên bản giao cho booking đã xác nhận.");
        }
        else
        {
            if (booking.Status != BookingStatus.DangThue)
                throw new ConflictException("INVALID_STATUS", "Chỉ lập biên bản nhận cho booking đang thuê.");
            var giao = await _handovers.GetByBookingAsync(bookingId, HandoverType.Giao)
                ?? throw new ConflictException("HANDOVER_REQUIRED", "Booking chưa có biên bản giao xe.");
            if (request.Odometer < giao.Odometer)
                throw new BadRequestException("ODOMETER_DECREASED", $"Số km lúc nhận không được nhỏ hơn lúc giao ({giao.Odometer}).");
        }

        if (await _handovers.ExistsAsync(bookingId, request.Type)) throw Duplicate();

        var record = new HandoverRecord
        {
            BookingId = bookingId, Type = request.Type, Odometer = request.Odometer, FuelLevel = request.FuelLevel,
            ConditionNote = request.ConditionNote?.Trim(), StaffId = staffId
        };
        await _handovers.AddAsync(record);
        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Unique (BookingId, Type) chặn request song song; lỗi khác ném lại nguyên.
            if (await _handovers.ExistsAsync(bookingId, request.Type)) throw Duplicate();
            throw;
        }
        return ToDto(record);
    }

    public async Task<HandoverPhotoDto> AddPhotoAsync(int handoverId, IFormFile file)
    {
        var record = await _handovers.GetByIdAsync(handoverId) ?? throw new NotFoundException("Biên bản", handoverId);
        if (record.Photos.Count >= MaxPhotos)
            throw new ConflictException("HANDOVER_PHOTO_LIMIT", $"Mỗi biên bản tối đa {MaxPhotos} ảnh.");

        var booking = await _bookings.GetByIdAsync(record.BookingId) ?? throw new NotFoundException("Booking", record.BookingId);
        // Đơn đã hủy/đã trả chốt biên bản; riêng biên bản Nhan còn được bổ sung ảnh lúc trả xe.
        if (booking.Status == BookingStatus.DaHuy || (booking.Status == BookingStatus.DaTraXe && record.Type != HandoverType.Nhan))
            throw new ConflictException("INVALID_STATUS", "Đơn đã hủy hoặc đã trả xe, không thể thêm ảnh vào biên bản này.");

        // Ảnh ở prefix private/, WebP tối đa 5 MB (kiểm bởi storage), chỉ đọc qua API có kiểm quyền.
        var key = await _files.SaveAsync(file, $"private/handovers/{record.BookingId}");
        var photo = new HandoverPhoto { HandoverRecordId = record.Id, ObjectKey = key };
        await _handovers.AddPhotoAsync(photo);
        await _unitOfWork.SaveChangesAsync();
        return new HandoverPhotoDto(photo.Id);
    }

    public async Task<List<HandoverDto>> ListAsync(int bookingId, int? customerId = null)
    {
        await EnsureAccessAsync(bookingId, customerId);
        return (await _handovers.ListByBookingAsync(bookingId)).Select(ToDto).ToList();
    }

    public async Task<HandoverPhotoFile> GetPhotoAsync(int photoId, int? customerId = null)
    {
        var photo = await _handovers.GetPhotoAsync(photoId) ?? throw new NotFoundException("Ảnh biên bản", photoId);
        await EnsureAccessAsync(photo.HandoverRecord!.BookingId, customerId);
        var (content, contentType) = await _files.OpenReadAsync(photo.ObjectKey);
        return new HandoverPhotoFile(content, contentType);
    }

    private async Task EnsureAccessAsync(int bookingId, int? customerId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId) ?? throw new NotFoundException("Booking", bookingId);
        if (customerId.HasValue && booking.CustomerId != customerId.Value)
            throw new ForbiddenException("Không có quyền xem biên bản của booking này");
    }

    private static ConflictException Duplicate()
        => new("HANDOVER_EXISTS", "Booking đã có biên bản loại này.");

    private static HandoverDto ToDto(HandoverRecord r) => new(
        r.Id, r.BookingId, r.Type, r.Odometer, r.FuelLevel, r.ConditionNote, r.StaffId, r.CreatedAt,
        r.Photos.OrderBy(p => p.Id).Select(p => new HandoverPhotoDto(p.Id)).ToList());
}
