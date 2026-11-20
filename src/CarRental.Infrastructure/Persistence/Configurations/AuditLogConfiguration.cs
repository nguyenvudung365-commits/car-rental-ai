using CarRental.Domain.Modules.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.Property(a => a.ActorRole).HasMaxLength(20);
        b.Property(a => a.Action).HasMaxLength(100).IsRequired();
        b.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        b.Property(a => a.EntityId).HasMaxLength(50);
        b.Property(a => a.IpAddress).HasMaxLength(64);
        b.HasIndex(a => new { a.EntityType, a.EntityId });
        b.HasIndex(a => a.CreatedAt);
    }
}
