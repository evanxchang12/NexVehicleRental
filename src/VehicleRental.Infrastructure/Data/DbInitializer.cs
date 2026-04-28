using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.Interfaces;
using VehicleRental.Domain.Entities;

namespace VehicleRental.Infrastructure.Data;

public class DbInitializer : IDbInitializer
{
    private readonly IAppDbContext _context;

    public DbInitializer(IAppDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        if (await _context.VehicleTypes.AnyAsync())
            return;

        var seedVehicles = new[]
        {
            new VehicleType { Id = 1, Name = "Toyota Camry 2024",   Description = "舒適四門轎車，適合商務出行",    DailyRate = 1800m, IsAvailable = true },
            new VehicleType { Id = 2, Name = "Mazda CX-5 2024",     Description = "時尚休旅車，空間寬敞",          DailyRate = 2200m, IsAvailable = true },
            new VehicleType { Id = 3, Name = "BMW 3 Series 2024",   Description = "豪華性能轎車，駕駛快感十足",    DailyRate = 3500m, IsAvailable = true },
            new VehicleType { Id = 4, Name = "Toyota Hiace Van",    Description = "商用廂型車，載貨載人皆宜",      DailyRate = 2500m, IsAvailable = true },
        };

        _context.VehicleTypes.AddRange(seedVehicles);
        await _context.SaveChangesAsync();
    }
}
