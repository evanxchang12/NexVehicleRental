using MediatR;
using VehicleRental.Application.DTOs.Results;

namespace VehicleRental.Application.Commands.CreateReservation;

public record CreateReservationCommand(
    int CustomerId,
    int VehicleTypeId,
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<CreateReservationResult>;
