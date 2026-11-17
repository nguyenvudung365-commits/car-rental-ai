namespace CarRental.Application.Modules.Bookings;

/// <summary>Dữ liệu in lên hợp đồng thuê xe.</summary>
public record ContractData(
    int BookingId,
    string BookingStatus,
    DateTime CreatedAt,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    string? CustomerIdentityNumber,
    string? CustomerAddress,
    string CarName,
    string LicensePlate,
    int Seats,
    DateTime StartDate,
    DateTime EndDate,
    int RentalDays,
    decimal FinalPricePerDay,
    decimal TotalPrice,
    DateTime? ReturnedAt,
    int? LateDays,
    decimal? LateFee,
    decimal? DamageFee,
    decimal? FinalAmount
);

public record ContractFile(byte[] Content, string FileName);

/// <summary>Sinh file PDF hợp đồng; triển khai ở Infrastructure.</summary>
public interface IContractPdfGenerator
{
    byte[] Generate(ContractData data);
}
