namespace VehicleRental.Application.DTOs.Results;

public record CalculateRentalCostResult(decimal TotalCost, int Days, decimal DailyRate);
