using CarRental.Domain.Modules.Cars;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class CarMaintenanceConfiguration : IEntityTypeConfiguration<CarMaintenance>
{
    public void Configure(EntityTypeBuilder<CarMaintenance> b)
    {
        b.ToTable("CarMaintenances");
        b.Property(m => m.Cost).HasPrecision(18, 2);
        b.Property(m => m.Note).HasMaxLength(500);
        b.HasOne(m => m.Car).WithMany().HasForeignKey(m => m.CarId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(m => new { m.CarId, m.Status });
        b.HasIndex(m => m.DueDate);
    }
}
