using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Branches;

/// <summary>Chi nhánh/bãi xe. Xe thuộc một chi nhánh (Car.BranchId).</summary>
public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
}
