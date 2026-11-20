namespace CarRental.Application.Modules.Customers;

/// <summary>Cửa duy nhất để module khác hỏi tình trạng blacklist của khách.</summary>
public interface ICustomerScreening
{
    Task<ScreeningResult> ScreenAsync(int customerId);
}
