using CarRental.Domain.Modules.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class DepositTransactionConfiguration : IEntityTypeConfiguration<DepositTransaction>
{
    public void Configure(EntityTypeBuilder<DepositTransaction> b)
    {
        b.ToTable("DepositTransactions");
        b.Property(t => t.Amount).HasPrecision(18, 2);
        b.Property(t => t.Note).HasMaxLength(500);
        b.HasOne(t => t.Booking).WithMany().HasForeignKey(t => t.BookingId).OnDelete(DeleteBehavior.Restrict);
        // Mỗi đơn chỉ thu, khấu trừ và hoàn cọc tối đa một lần.
        b.HasIndex(t => new { t.BookingId, t.Type }).IsUnique();
    }
}
