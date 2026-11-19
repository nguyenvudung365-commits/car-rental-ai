using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Branches;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> b)
    {
        b.ToTable("Branches");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Address).HasMaxLength(300).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20);
    }
}

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.ToTable("Coupons");
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.Property(x => x.Value).HasPrecision(18, 2);
        b.Property(x => x.MaxDiscount).HasPrecision(18, 2);
        b.Property(x => x.MinOrderAmount).HasPrecision(18, 2);
        // Mã duy nhất: chặn tạo trùng khi đồng thời. UsedCount chỉ đổi bằng UPDATE có điều kiện.
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class SurchargeConfiguration : IEntityTypeConfiguration<Surcharge>
{
    public void Configure(EntityTypeBuilder<Surcharge> b)
    {
        b.ToTable("Surcharges");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2);
    }
}

public class BookingChargeConfiguration : IEntityTypeConfiguration<BookingCharge>
{
    public void Configure(EntityTypeBuilder<BookingCharge> b)
    {
        b.ToTable("BookingCharges");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasOne(x => x.Booking).WithMany(x => x.Charges).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Coupon>().WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Surcharge>().WithMany().HasForeignKey(x => x.SurchargeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.BookingId);
        b.HasIndex(x => x.CouponId);
    }
}

public class CarBranchConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> b)
    {
        b.Property(x => x.PricePerHour).HasPrecision(18, 2);
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.BranchId);
    }
}

public class BookingModeConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> b)
    {
        b.Property(x => x.DeliveryAddress).HasMaxLength(300);
        b.Property(x => x.ReturnAddress).HasMaxLength(300);
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.PickupBranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.ReturnBranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
