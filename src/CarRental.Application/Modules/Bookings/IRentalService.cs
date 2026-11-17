namespace CarRental.Application.Modules.Bookings;

/// <summary>Vòng đời thuê sau khi xác nhận: thanh toán, nhận xe, trả xe.</summary>
public interface IRentalService
{
    Task<PaymentDto> CreatePaymentAsync(int bookingId, CreatePaymentRequest request, int? customerId = null);
    Task<BookingPaymentsDto> GetPaymentsAsync(int bookingId, int? customerId = null);
    Task<PaymentDto> MarkPaymentPaidAsync(int paymentId, MarkPaymentPaidRequest request);
    Task<BookingDto> PickupAsync(int bookingId);
    Task<ReturnRecordDto> ReturnAsync(int bookingId, ReturnBookingRequest request, int? staffId);
    Task<ReturnRecordDto> GetReturnAsync(int bookingId, int? customerId = null);
    Task<ContractFile> GetContractAsync(int bookingId, int? customerId = null);
}
