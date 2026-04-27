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
            entity.Property(e => e.DailyRate).HasColumnType("decimal(18,2)").IsRequired();

            entity.HasData(
                new VehicleType { Id = 1, Name = "Toyota Camry 2024", Description = "舒適四門轎車，適合商務出行", DailyRate = 1800m, IsAvailable = true, ImageUrl = "/image/Toyota_Camry_2024.png" },
                new VehicleType { Id = 2, Name = "Mazda CX-5 2024", Description = "時尚休旅車，空間寬敞", DailyRate = 2200m, IsAvailable = true, ImageUrl = "/image/Mazda_CX-5_2024.png" },
                new VehicleType { Id = 3, Name = "BMW 3 Series 2024", Description = "豪華性能轎車，駕駛快感十足", DailyRate = 3500m, IsAvailable = true, ImageUrl = "/image/BMW_3_Series_2024.png" },
                new VehicleType { Id = 4, Name = "Toyota Hiace Van", Description = "商用廂型車，載貨載人皆宜", DailyRate = 2500m, IsAvailable = true, ImageUrl = "/image/Toyota_Hiace_Van.png" }
            );
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(e => e.ReservationNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(e => e.ReservationNumber).IsUnique();
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)").IsRequired();

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
