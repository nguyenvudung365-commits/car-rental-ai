using CarRental.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class BlacklistEntryConfiguration : IEntityTypeConfiguration<BlacklistEntry>
{
    public void Configure(EntityTypeBuilder<BlacklistEntry> b)
    {
        b.ToTable("BlacklistEntries");
        b.Property(e => e.IdentityNumber).HasMaxLength(20);
        b.Property(e => e.PhoneNumber).HasMaxLength(20);
        b.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        b.Property(e => e.RemoveReason).HasMaxLength(500);
        b.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => e.CustomerId);
        b.HasIndex(e => e.IdentityNumber);
        b.HasIndex(e => e.PhoneNumber);
    }
}
