namespace VehicleRental.Application.DTOs.Results;

public record LoginResult(bool Success, int? CustomerId, string? FullName);
