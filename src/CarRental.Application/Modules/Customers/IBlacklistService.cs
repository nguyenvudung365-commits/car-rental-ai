namespace CarRental.Application.Modules.Customers;

public interface IBlacklistService
{
    Task<(List<BlacklistEntryDto> Items, int TotalCount)> ListAsync(BlacklistFilterRequest filter);
    Task<BlacklistEntryDto> CreateAsync(CreateBlacklistRequest request);
    Task RemoveAsync(int id, RemoveBlacklistRequest request);
}
