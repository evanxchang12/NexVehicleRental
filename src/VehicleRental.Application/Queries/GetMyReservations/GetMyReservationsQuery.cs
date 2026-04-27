using MediatR;
using VehicleRental.Application.DTOs;

namespace VehicleRental.Application.Queries.GetMyReservations;

public record GetMyReservationsQuery(int CustomerId) : IRequest<IEnumerable<ReservationSummaryDto>>;
