using Microsoft.EntityFrameworkCore;
using VehicleRental.Domain.Entities;

namespace VehicleRental.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<VehicleType> VehicleTypes { get; }
    DbSet<Reservation> Reservations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
