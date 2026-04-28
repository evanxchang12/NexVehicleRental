using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleRental.Application.Interfaces;
using VehicleRental.Infrastructure.Data;

namespace VehicleRental.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core InMemory + DbInitializer for Blazor WASM.
    /// Call AddWasmServices() in VehicleRental.Wasm for LocalStoragePersistenceService.
    /// </summary>
    public static IServiceCollection AddInfrastructureWasm(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("VehicleRentalDb"),
            ServiceLifetime.Singleton);

        services.AddSingleton<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IDbInitializer, DbInitializer>();

        return services;
    }
}


