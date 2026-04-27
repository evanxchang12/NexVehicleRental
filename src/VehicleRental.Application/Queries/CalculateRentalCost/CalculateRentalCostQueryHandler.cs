using MediatR;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.DTOs.Results;
using VehicleRental.Application.Interfaces;

namespace VehicleRental.Application.Queries.CalculateRentalCost;

public class CalculateRentalCostQueryHandler : IRequestHandler<CalculateRentalCostQuery, CalculateRentalCostResult?>
{
    private readonly IAppDbContext _context;

    public CalculateRentalCostQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<CalculateRentalCostResult?> Handle(CalculateRentalCostQuery request, CancellationToken cancellationToken)
    {
        var vehicleType = await _context.VehicleTypes
            .FirstOrDefaultAsync(v => v.Id == request.VehicleTypeId, cancellationToken);

        if (vehicleType is null)
            return null;

        var days = request.EndDate.DayNumber - request.StartDate.DayNumber;
        if (days <= 0)
            return new CalculateRentalCostResult(0, 0, vehicleType.DailyRate);

        var totalCost = vehicleType.DailyRate * days;
        return new CalculateRentalCostResult(totalCost, days, vehicleType.DailyRate);
    }
}
