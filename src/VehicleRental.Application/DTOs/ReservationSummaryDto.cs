namespace VehicleRental.Application.DTOs;

public record ReservationSummaryDto(
    int Id,
    string ReservationNumber,
    string VehicleTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalCost,
    string Status,
    bool CanCancel);
