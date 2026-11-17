using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;

namespace CarRental.Application.Modules.Bookings;

public class RentalService : IRentalService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingService _bookingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContractPdfGenerator _contractPdf;

    public RentalService(
        IBookingRepository bookingRepository,
        IBookingService bookingService,
        IUnitOfWork unitOfWork,
        IContractPdfGenerator contractPdf)
    {
        _bookingRepository = bookingRepository;
        _bookingService = bookingService;
        _unitOfWork = unitOfWork;
        _contractPdf = contractPdf;
    }

    public async Task<PaymentDto> CreatePaymentAsync(int bookingId, CreatePaymentRequest request, int? customerId = null)
    {
        var booking = await LoadAsync(bookingId, customerId);

        if (booking.Status is BookingStatus.ChoXacNhan or BookingStatus.DaHuy)
        {
            throw new ConflictException("INVALID_STATUS", "Chỉ thanh toán cho booking đã xác nhận");
        }

        var due = AmountDue(booking);
        if (request.Amount > due)
        {
            throw new ConflictException("PAYMENT_EXCEEDS_DUE", $"Số tiền vượt quá số còn phải thanh toán ({due:N0})");
        }

        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.ChuaThanhToan,
            TransactionCode = request.TransactionCode,
            CreatedAt = DateTime.UtcNow
        };

        await _bookingRepository.AddPaymentAsync(payment);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(payment);
    }

    public async Task<BookingPaymentsDto> GetPaymentsAsync(int bookingId, int? customerId = null)
    {
        var booking = await LoadAsync(bookingId, customerId);
        var amountToPay = booking.ReturnRecord?.FinalAmount ?? booking.TotalPrice;
        var paid = booking.Payments
            .Where(p => p.Status is PaymentStatus.DaCoc or PaymentStatus.DaThanhToan)
            .Sum(p => p.Amount);

        return new BookingPaymentsDto(
            booking.Payments.OrderBy(p => p.CreatedAt).Select(ToDto).ToList(),
            booking.TotalPrice,
            amountToPay,
            paid,
            Math.Max(0, amountToPay - paid));
    }

    public async Task<PaymentDto> MarkPaymentPaidAsync(int paymentId, MarkPaymentPaidRequest request)
    {
        if (request.Status is not (PaymentStatus.DaCoc or PaymentStatus.DaThanhToan))
        {
            throw new BadRequestException("Status chỉ nhận DaCoc (2) hoặc DaThanhToan (3)");
        }

        var payment = await _bookingRepository.GetPaymentByIdAsync(paymentId)
            ?? throw new NotFoundException("Payment", paymentId);

        if (payment.Status != PaymentStatus.ChuaThanhToan)
        {
            throw new ConflictException("INVALID_STATUS", "Chỉ ghi nhận cho khoản chưa thanh toán");
        }

        payment.Status = request.Status;
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.TransactionCode))
        {
            payment.TransactionCode = request.TransactionCode;
        }

        await _unitOfWork.SaveChangesAsync();
        return ToDto(payment);
    }

    public async Task<BookingDto> PickupAsync(int bookingId)
    {
        var booking = await LoadAsync(bookingId);
        if (booking.Status != BookingStatus.DaXacNhan)
        {
            throw new ConflictException("INVALID_STATUS", "Chỉ giao xe cho booking đã xác nhận");
        }

        booking.Status = BookingStatus.DangThue;
        if (booking.Car != null) booking.Car.Status = CarStatus.DangThue;
        booking.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return await _bookingService.GetByIdAsync(bookingId);
    }

    public async Task<ReturnRecordDto> ReturnAsync(int bookingId, ReturnBookingRequest request, int? staffId)
    {
        var booking = await LoadAsync(bookingId);
        if (booking.ReturnRecord != null)
        {
            throw new ConflictException("RETURN_EXISTS", "Booking đã có biên bản trả xe");
        }

        if (booking.Status != BookingStatus.DangThue)
        {
            throw new ConflictException("INVALID_STATUS", "Chỉ nhận trả xe với booking đang thuê");
        }

        var returnedAt = request.ReturnedAt ?? DateTime.UtcNow;
        if (returnedAt.Date < booking.StartDate.Date)
        {
            throw new BadRequestException("Ngày trả không được trước ngày bắt đầu thuê");
        }

        var (lateDays, lateFee, finalAmount) = CalculateReturn(
            booking.EndDate, returnedAt, booking.FinalPricePerDay, booking.TotalPrice, request.DamageFee);

        var record = new ReturnRecord
        {
            BookingId = booking.Id,
            ReturnedAt = returnedAt,
            LateDays = lateDays,
            LateFee = lateFee,
            DamageFee = request.DamageFee,
            DamageNote = request.DamageNote,
            FinalAmount = finalAmount,
            StaffId = staffId,
            CreatedAt = DateTime.UtcNow
        };

        booking.Status = BookingStatus.DaTraXe;
        if (booking.Car is { Status: CarStatus.DangThue }) booking.Car.Status = CarStatus.SanSang;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.AddReturnRecordAsync(record);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(record, booking);
    }

    public async Task<ReturnRecordDto> GetReturnAsync(int bookingId, int? customerId = null)
    {
        var booking = await LoadAsync(bookingId, customerId);
        return booking.ReturnRecord == null
            ? throw new NotFoundException("ReturnRecord của booking", bookingId)
            : ToDto(booking.ReturnRecord, booking);
    }

    /// <summary>Hợp đồng PDF cho booking từ DaXacNhan trở đi (không gồm ChoXacNhan/DaHuy). Customer chỉ tải booking của mình.</summary>
    public async Task<ContractFile> GetContractAsync(int bookingId, int? customerId = null)
    {
        var booking = await LoadAsync(bookingId, customerId);
        if (booking.Status is BookingStatus.ChoXacNhan or BookingStatus.DaHuy)
        {
            throw new ConflictException("INVALID_STATUS", "Chỉ có hợp đồng cho booking đã xác nhận trở đi");
        }

        var c = booking.Customer;
        var car = booking.Car;
        var r = booking.ReturnRecord;
        var data = new ContractData(
            booking.Id, booking.Status.ToString(), booking.CreatedAt,
            c?.FullName ?? "", c?.Email ?? "", c?.PhoneNumber ?? "", c?.IdentityNumber, c?.Address,
            car == null ? "" : $"{car.Brand} {car.Model}", car?.LicensePlate ?? "", car?.Seats ?? 0,
            booking.StartDate, booking.EndDate, (booking.EndDate - booking.StartDate).Days,
            booking.FinalPricePerDay, booking.TotalPrice,
            r?.ReturnedAt, r?.LateDays, r?.LateFee, r?.DamageFee, r?.FinalAmount);

        return new ContractFile(_contractPdf.Generate(data), $"hop-dong-{booking.Id}.pdf");
    }

    /// <summary>LateDays = số ngày lịch trả sau EndDate; LateFee = LateDays × giá chốt/ngày; FinalAmount = TotalPrice + LateFee + DamageFee.</summary>
    public static (int LateDays, decimal LateFee, decimal FinalAmount) CalculateReturn(
        DateTime endDate, DateTime returnedAt, decimal finalPricePerDay, decimal totalPrice, decimal damageFee)
    {
        var lateDays = Math.Max(0, (returnedAt.Date - endDate.Date).Days);
        var lateFee = lateDays * finalPricePerDay;
        return (lateDays, lateFee, totalPrice + lateFee + damageFee);
    }

    private static decimal AmountDue(Booking booking)
    {
        var amountToPay = booking.ReturnRecord?.FinalAmount ?? booking.TotalPrice;
        var committed = booking.Payments.Where(p => p.Status != PaymentStatus.DaHoanTien).Sum(p => p.Amount);
        return Math.Max(0, amountToPay - committed);
    }

    private async Task<Booking> LoadAsync(int bookingId, int? customerId = null)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, includeDetails: true)
            ?? throw new NotFoundException("Booking", bookingId);

        if (customerId.HasValue && booking.CustomerId != customerId.Value)
        {
            throw new ForbiddenException("Không có quyền thao tác booking này");
        }

        return booking;
    }

    private static PaymentDto ToDto(Payment p) => new(
        p.Id, p.BookingId, p.Amount, p.Method.ToString(), p.Status.ToString(), p.TransactionCode, p.PaidAt, p.CreatedAt);

    private static ReturnRecordDto ToDto(ReturnRecord r, Booking b) => new(
        r.BookingId, r.ReturnedAt, b.EndDate, r.LateDays, r.LateFee, r.DamageFee, r.DamageNote,
        b.TotalPrice, r.FinalAmount, r.StaffId);
}
