using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;

namespace CarRental.Application.Modules.Customers;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documents;
    private readonly ICustomerRepository _customers;
    private readonly IFileStorageService _files;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DocumentService(IDocumentRepository documents, ICustomerRepository customers, IFileStorageService files,
        IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _documents = documents;
        _customers = customers;
        _files = files;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<List<DocumentDto>> GetMineAsync(int customerId)
        => (await _documents.GetByCustomerAsync(customerId)).Select(d => ToDto(d)).ToList();

    public async Task<DocumentDto> SubmitAsync(int customerId, SubmitDocumentRequest request)
    {
        if (!Enum.IsDefined(request.Type))
            throw new BadRequestException("INVALID_DOCUMENT_TYPE", "Loại giấy tờ không hợp lệ.");
        var number = IdentityNormalizer.Cccd(request.Number);
        if (number is not { Length: 12 })
            throw new BadRequestException("INVALID_DOCUMENT_NUMBER", "Số CCCD/bằng lái phải gồm đúng 12 chữ số.");
        var isLicense = request.Type == DocumentType.BangLai;
        if (isLicense && (request.ExpiryDate is null || request.ExpiryDate.Value.Date <= DateTime.UtcNow.Date))
            throw new BadRequestException("INVALID_EXPIRY", "Bằng lái phải còn hạn (ngày hết hạn ở tương lai).");

        // Ảnh lưu ở prefix private/: không đọc công khai, chỉ truy cập qua API có kiểm quyền.
        var folder = $"private/documents/{customerId}";
        var document = new CustomerDocument
        {
            CustomerId = customerId,
            Type = request.Type,
            Number = number,
            ExpiryDate = isLicense ? request.ExpiryDate!.Value.Date : null,
            LicenseClass = isLicense ? request.LicenseClass?.Trim() : null,
            FrontKey = await _files.SaveAsync(request.Front, folder),
            BackKey = request.Back is null ? null : await _files.SaveAsync(request.Back, folder)
        };

        // Soft-delete bản cũ trước khi thêm bản mới (unique index lọc IsDeleted = 0). Giữ file cũ để đối soát.
        int? oldId = null;
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var old = await _documents.GetActiveAsync(customerId, request.Type);
                oldId = old?.Id;
                if (old is not null) { old.IsDeleted = true; await _unitOfWork.SaveChangesAsync(); }
                await _documents.AddAsync(document);
                await _unitOfWork.SaveChangesAsync();
                return 0;
            });
        }
        catch (Exception)
        {
            // Nộp đồng thời: request kia đã thay giấy tờ, unique index chặn request này. Lỗi khác thì ném lại nguyên.
            if (!await ReplacedByOtherRequestAsync(customerId, request.Type, oldId)) throw;
            throw new ConflictException("DOCUMENT_SUBMIT_CONFLICT", "Giấy tờ vừa được nộp ở yêu cầu khác, vui lòng tải lại và thử lại.");
        }
        return ToDto(document);
    }

    private async Task<bool> ReplacedByOtherRequestAsync(int customerId, DocumentType type, int? oldId)
    {
        var current = await _documents.GetActiveAsync(customerId, type);
        return current is not null && current.Id != oldId;
    }

    public async Task<(List<DocumentDto> Items, int TotalCount)> ListQueueAsync(DocumentFilterRequest filter)
    {
        var (items, total) = await _documents.GetQueueAsync(
            filter.Status, Math.Max(filter.Page, 1), Math.Clamp(filter.PageSize, 1, 100));
        return (items.Select(d => ToDto(d, withCustomer: true)).ToList(), total);
    }

    public async Task<DocumentDto> ApproveAsync(int id)
    {
        var doc = await LoadForReviewAsync(id);
        doc.Status = DocumentStatus.DaDuyet;
        doc.RejectReason = null;
        Stamp(doc);
        if (doc.Type == DocumentType.Cccd)
        {
            // Hợp đồng lấy CCCD từ hồ sơ khách: đồng bộ theo số đã được duyệt.
            var customer = await _customers.GetByIdAsync(doc.CustomerId);
            if (customer is not null) customer.IdentityNumber = doc.Number;
        }
        await _unitOfWork.SaveChangesAsync();
        return ToDto(doc, withCustomer: true);
    }

    public async Task<DocumentDto> RejectAsync(int id, RejectDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BadRequestException("REASON_REQUIRED", "Cần nhập lý do từ chối.");
        var doc = await LoadForReviewAsync(id);
        doc.Status = DocumentStatus.TuChoi;
        doc.RejectReason = request.Reason.Trim();
        Stamp(doc);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(doc, withCustomer: true);
    }

    public async Task<DocumentFile> GetFileAsync(int id, string side)
    {
        var doc = await _documents.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy giấy tờ.");
        if (!_currentUser.IsStaffOrAdmin && doc.CustomerId != _currentUser.UserId)
            throw new ForbiddenException("Không có quyền xem giấy tờ này.");

        var key = side?.ToLowerInvariant() switch
        {
            "front" => doc.FrontKey,
            "back" => doc.BackKey ?? throw new NotFoundException("Giấy tờ không có mặt sau."),
            _ => throw new BadRequestException("INVALID_SIDE", "side phải là front hoặc back.")
        };
        var (content, contentType) = await _files.OpenReadAsync(key);
        return new DocumentFile(content, contentType);
    }

    public async Task<bool> IsVerifiedForRentalAsync(int customerId, DateTime endDate)
    {
        var docs = await _documents.GetByCustomerAsync(customerId);
        var cccd = docs.FirstOrDefault(d => d.Type == DocumentType.Cccd);
        var license = docs.FirstOrDefault(d => d.Type == DocumentType.BangLai);
        return cccd?.Status == DocumentStatus.DaDuyet
            && license?.Status == DocumentStatus.DaDuyet
            && license.ExpiryDate is { } expiry && expiry.Date >= endDate.Date;
    }

    /// <summary>DaDuyet khi đủ cả hai loại đã duyệt; TuChoi nếu có giấy tờ bị từ chối; ChoDuyet nếu có giấy tờ đang chờ.</summary>
    public static DocumentsOverallStatus Overall(IEnumerable<CustomerDocument> docs)
    {
        var list = docs.ToList();
        if (list.Count(d => d.Status == DocumentStatus.DaDuyet) == 2) return DocumentsOverallStatus.DaDuyet;
        if (list.Any(d => d.Status == DocumentStatus.TuChoi)) return DocumentsOverallStatus.TuChoi;
        if (list.Any(d => d.Status == DocumentStatus.ChoDuyet)) return DocumentsOverallStatus.ChoDuyet;
        return DocumentsOverallStatus.ChuaNop;
    }

    public static DocumentDto ToDto(CustomerDocument d, bool withCustomer = false) => new(
        d.Id, d.CustomerId, withCustomer ? d.Customer?.FullName : null, d.Type, d.Number, d.ExpiryDate, d.LicenseClass,
        d.Status, d.RejectReason, d.ReviewedAt, d.BackKey is not null, d.CreatedAt);

    private async Task<CustomerDocument> LoadForReviewAsync(int id)
    {
        var doc = await _documents.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy giấy tờ.");
        if (doc.CustomerId == _currentUser.UserId)
            throw new ForbiddenException("SELF_REVIEW", "Không được tự duyệt giấy tờ của chính mình.");
        if (doc.Status != DocumentStatus.ChoDuyet)
            throw new ConflictException("INVALID_STATUS", "Giấy tờ đã được xử lý.");
        return doc;
    }

    private void Stamp(CustomerDocument doc)
    {
        doc.ReviewedById = _currentUser.UserId;
        doc.ReviewedAt = DateTime.UtcNow;
    }
}
