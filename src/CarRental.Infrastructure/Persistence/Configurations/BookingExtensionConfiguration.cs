using CarRental.Domain.Modules.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class BookingExtensionConfiguration : IEntityTypeConfiguration<BookingExtension>
{
    public void Configure(EntityTypeBuilder<BookingExtension> b)
    {
        b.ToTable("BookingExtensions");
        b.Property(e => e.ExtraAmount).HasPrecision(18, 2);
        b.Property(e => e.RejectReason).HasMaxLength(500);
        b.HasOne(e => e.Booking).WithMany().HasForeignKey(e => e.BookingId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => e.BookingId);
        // Mỗi đơn chỉ có một yêu cầu đang chờ duyệt.
        b.HasIndex(e => e.BookingId).IsUnique().HasFilter("[Status] = 1").HasDatabaseName("IX_BookingExtensions_BookingId_Pending");
    }
}
