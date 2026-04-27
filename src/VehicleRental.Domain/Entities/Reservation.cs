using VehicleRental.Domain.Enums;

namespace VehicleRental.Domain.Entities;

public class Reservation
{
    public int Id { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int VehicleTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalCost { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public DateTimeOffset CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public VehicleType VehicleType { get; set; } = null!;
}
