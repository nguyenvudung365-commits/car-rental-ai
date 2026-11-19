using CarRental.Domain.Modules.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class HandoverRecordConfiguration : IEntityTypeConfiguration<HandoverRecord>
{
    public void Configure(EntityTypeBuilder<HandoverRecord> b)
    {
        b.ToTable("HandoverRecords");
        b.Property(h => h.ConditionNote).HasMaxLength(1000);
        b.HasOne(h => h.Booking).WithMany().HasForeignKey(h => h.BookingId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(h => new { h.BookingId, h.Type }).IsUnique();
    }
}

public class HandoverPhotoConfiguration : IEntityTypeConfiguration<HandoverPhoto>
{
    public void Configure(EntityTypeBuilder<HandoverPhoto> b)
    {
        b.ToTable("HandoverPhotos");
        b.Property(p => p.ObjectKey).HasMaxLength(300).IsRequired();
        b.HasOne(p => p.HandoverRecord).WithMany(h => h.Photos).HasForeignKey(p => p.HandoverRecordId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => p.HandoverRecordId);
    }
}
