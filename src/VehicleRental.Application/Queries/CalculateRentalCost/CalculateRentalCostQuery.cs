using MediatR;
using VehicleRental.Application.DTOs.Results;

namespace VehicleRental.Application.Queries.CalculateRentalCost;

public record CalculateRentalCostQuery(
    int VehicleTypeId,
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<CalculateRentalCostResult?>;
