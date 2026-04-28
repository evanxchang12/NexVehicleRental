using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.Interfaces;
using VehicleRental.Domain.Entities;
using VehicleRental.Domain.Enums;

namespace VehicleRental.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DailyRate).IsRequired();
            // Note: HasColumnType("decimal(18,2)") omitted — InMemory provider ignores column type
            // Seed data is handled by DbInitializer, not HasData, for WASM compatibility
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(e => e.ReservationNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(e => e.ReservationNumber).IsUnique();
            entity.Property(e => e.TotalCost).IsRequired();
            // Note: HasColumnType("decimal(18,2)") omitted — InMemory provider ignores column type

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Reservations)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.VehicleType)
                  .WithMany(v => v.Reservations)
                  .HasForeignKey(e => e.VehicleTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
