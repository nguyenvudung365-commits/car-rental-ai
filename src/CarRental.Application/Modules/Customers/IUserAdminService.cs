namespace CarRental.Application.Modules.Customers;

/// <summary>Quản lý tài khoản (chỉ Admin): danh sách, tạo nhân viên, đổi vai trò, khóa/mở khóa.</summary>
public interface IUserAdminService
{
    Task<(List<UserSummaryDto> Items, int TotalCount)> ListAsync(UserListFilterRequest filter);
    Task<UserSummaryDto> CreateStaffAsync(CreateStaffRequest request);
    Task ChangeRoleAsync(int id, ChangeRoleRequest request);
    Task LockAsync(int id, LockUserRequest request);
    Task UnlockAsync(int id);
}
