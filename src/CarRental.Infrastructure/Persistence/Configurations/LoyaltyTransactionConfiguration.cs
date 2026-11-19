using CarRental.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> b)
    {
        b.ToTable("LoyaltyTransactions");
        b.Property(t => t.Note).HasMaxLength(500);
        b.HasOne(t => t.Customer).WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(t => t.CustomerId);
        // Không cộng trùng điểm cho cùng một đơn và cùng lý do.
        b.HasIndex(t => new { t.BookingId, t.Reason }).IsUnique().HasFilter("[BookingId] IS NOT NULL");
    }
}
