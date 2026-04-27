namespace VehicleRental.Application.DTOs;

public record VehicleTypeDto(
    int Id,
    string Name,
    string Description,
    decimal DailyRate,
    bool IsAvailable,
    string? ImageUrl);
