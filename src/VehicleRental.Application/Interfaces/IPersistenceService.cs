namespace VehicleRental.Application.Interfaces;

public interface IPersistenceService
{
    Task SaveAsync();
    Task RestoreAsync();
}
