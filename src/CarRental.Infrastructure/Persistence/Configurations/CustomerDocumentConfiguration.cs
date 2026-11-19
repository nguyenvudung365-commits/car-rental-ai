using CarRental.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class CustomerDocumentConfiguration : IEntityTypeConfiguration<CustomerDocument>
{
    public void Configure(EntityTypeBuilder<CustomerDocument> b)
    {
        b.ToTable("CustomerDocuments");
        b.Property(d => d.Number).HasMaxLength(30).IsRequired();
        b.Property(d => d.FrontKey).HasMaxLength(300).IsRequired();
        b.Property(d => d.BackKey).HasMaxLength(300);
        b.Property(d => d.LicenseClass).HasMaxLength(10);
        b.Property(d => d.RejectReason).HasMaxLength(500);
        b.Property(d => d.ExpiryDate).HasColumnType("date");
        b.HasOne(d => d.Customer).WithMany().HasForeignKey(d => d.CustomerId).OnDelete(DeleteBehavior.Restrict);
        // Mỗi khách tối đa một giấy tờ hiệu lực cho mỗi loại.
        b.HasIndex(d => new { d.CustomerId, d.Type }).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(d => d.Status);
    }
}
