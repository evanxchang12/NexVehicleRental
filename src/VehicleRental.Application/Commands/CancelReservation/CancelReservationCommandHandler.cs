using MediatR;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Domain.Enums;
using VehicleRental.Infrastructure.Data;

namespace VehicleRental.Application.Commands.CancelReservation;

public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, bool>
{
    private readonly AppDbContext _context;

    public CancelReservationCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId
                                   && r.CustomerId == request.CustomerId,
                                 cancellationToken);

        if (reservation is null)
            return false;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (reservation.StartDate <= today)
            return false;

        if (reservation.Status != ReservationStatus.Confirmed)
            return false;

        reservation.Status = ReservationStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
