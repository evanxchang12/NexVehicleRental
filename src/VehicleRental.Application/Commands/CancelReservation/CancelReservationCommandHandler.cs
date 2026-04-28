using MediatR;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.Interfaces;
using VehicleRental.Domain.Enums;

namespace VehicleRental.Application.Commands.CancelReservation;

public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, bool>
{
    private readonly IAppDbContext _context;
    private readonly IPersistenceService? _persistence;

    public CancelReservationCommandHandler(IAppDbContext context, IPersistenceService? persistence = null)
    {
        _context = context;
        _persistence = persistence;
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

        if (_persistence is not null)
            await _persistence.SaveAsync();

        return true;
    }
}

