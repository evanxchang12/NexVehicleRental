namespace VehicleRental.Application.DTOs.Results;

public record CreateReservationResult(bool Success, int ReservationId, string? Error);
