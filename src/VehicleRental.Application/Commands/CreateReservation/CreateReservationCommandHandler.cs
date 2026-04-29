using MediatR;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.DTOs.Results;
using VehicleRental.Application.Interfaces;
using VehicleRental.Domain.Entities;
using VehicleRental.Domain.Enums;

namespace VehicleRental.Application.Commands.CreateReservation;

public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, CreateReservationResult>
{
    private readonly IAppDbContext _context;
    private readonly IPersistenceService? _persistence;

    public CreateReservationCommandHandler(IAppDbContext context, IPersistenceService? persistence = null)
    {
        _context = context;
        _persistence = persistence;
    }

    public async Task<CreateReservationResult> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
            return new CreateReservationResult(false, 0, "結束日期不可早於起始日期");

        var vehicleType = await _context.VehicleTypes
            .FirstOrDefaultAsync(v => v.Id == request.VehicleTypeId, cancellationToken);

        if (vehicleType is null || !vehicleType.IsAvailable)
            return new CreateReservationResult(false, 0, "所選車型不可用");

        var hasConflict = await _context.Reservations
            .AnyAsync(r => r.VehicleTypeId == request.VehicleTypeId
                        && r.Status == ReservationStatus.Confirmed
                        && r.StartDate < request.EndDate
                        && r.EndDate > request.StartDate,
                      cancellationToken);

        if (hasConflict)
            return new CreateReservationResult(false, 0, "此時段車輛已被預約，請選擇其他時間");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayCount = await _context.Reservations
            .CountAsync(r => r.CreatedAt.Date == DateTime.UtcNow.Date, cancellationToken);

        var reservationNumber = $"RES-{today:yyyyMMdd}-{todayCount + 1:D4}";

        var days = Math.Max(1, request.EndDate.DayNumber - request.StartDate.DayNumber);
        var totalCost = vehicleType.DailyRate * days;

        var reservation = new Reservation
        {
            ReservationNumber = reservationNumber,
            CustomerId = request.CustomerId,
            VehicleTypeId = request.VehicleTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalCost = totalCost,
            Status = ReservationStatus.Confirmed,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync(cancellationToken);

        if (_persistence is not null)
            await _persistence.SaveAsync();

        return new CreateReservationResult(true, reservation.Id, null);
    }
}

