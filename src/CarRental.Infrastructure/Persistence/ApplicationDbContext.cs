using CarRental.Application.Common;
using CarRental.Domain.Modules.Bookings;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = CarRental.Domain.Modules.Customers.Customer;
using CarEntity = CarRental.Domain.Modules.Cars.Car;
using CarImageEntity = CarRental.Domain.Modules.Cars.CarImage;

namespace CarRental.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<CarEntity> Cars => Set<CarEntity>();
    public DbSet<CarImageEntity> CarImages => Set<CarImageEntity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ReturnRecord> ReturnRecords => Set<ReturnRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerEntity>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<CustomerEntity>()
            .HasIndex(c => c.PhoneNumber)
            .IsUnique();

        modelBuilder.Entity<CarEntity>()
            .HasIndex(c => c.LicensePlate)
            .IsUnique();

        modelBuilder.Entity<Booking>()
            .HasMany(b => b.Payments)
            .WithOne(p => p.Booking)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.ReturnRecord)
            .WithOne(r => r.Booking)
            .HasForeignKey<ReturnRecord>(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}