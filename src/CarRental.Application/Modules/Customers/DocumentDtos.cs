using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace CarRental.Application.Modules.Customers;

/// <summary>Không bao giờ trả object key của ảnh: ảnh chỉ lấy qua GET api/documents/{id}/file.</summary>
public record DocumentDto(
    int Id,
    int CustomerId,
    string? CustomerName,
    DocumentType Type,
    string Number,
    DateTime? ExpiryDate,
    string? LicenseClass,
    DocumentStatus Status,
    string? RejectReason,
    DateTime? ReviewedAt,
    bool HasBack,
    DateTime CreatedAt);

public record SubmitDocumentRequest(
    DocumentType Type,
    [Required, MaxLength(30)] string Number,
    DateTime? ExpiryDate,
    [MaxLength(10)] string? LicenseClass,
    [Required] IFormFile Front,
    IFormFile? Back);

public record RejectDocumentRequest([MaxLength(500)] string? Reason);

public record DocumentFilterRequest(DocumentStatus? Status, int Page = 1, int PageSize = 20);

public record DocumentFile(Stream Content, string ContentType);
