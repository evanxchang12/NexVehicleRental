namespace VehicleRental.Domain.Entities;

public class VehicleType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? ImageUrl { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = [];
}
