using MediatR;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.DTOs;
using VehicleRental.Application.Interfaces;
using VehicleRental.Domain.Enums;

namespace VehicleRental.Application.Queries.GetMyReservations;

public class GetMyReservationsQueryHandler : IRequestHandler<GetMyReservationsQuery, IEnumerable<ReservationSummaryDto>>
{
    private readonly IAppDbContext _context;

    public GetMyReservationsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReservationSummaryDto>> Handle(GetMyReservationsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _context.Reservations
            .Where(r => r.CustomerId == request.CustomerId)
            .Include(r => r.VehicleType)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReservationSummaryDto(
                r.Id,
                r.ReservationNumber,
                r.VehicleType.Name,
                r.StartDate,
                r.EndDate,
                r.TotalCost,
                r.Status == ReservationStatus.Confirmed ? "已確認" : "已取消",
                r.Status == ReservationStatus.Confirmed && r.StartDate > today))
            .ToListAsync(cancellationToken);
    }
}
