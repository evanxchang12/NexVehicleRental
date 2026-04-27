using MediatR;

namespace VehicleRental.Application.Commands.CancelReservation;

public record CancelReservationCommand(int ReservationId, int CustomerId) : IRequest<bool>;
