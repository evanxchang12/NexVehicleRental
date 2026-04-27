using MediatR;
using VehicleRental.Application.DTOs;

namespace VehicleRental.Application.Queries.GetVehicleTypes;

public record GetVehicleTypesQuery : IRequest<IEnumerable<VehicleTypeDto>>;
