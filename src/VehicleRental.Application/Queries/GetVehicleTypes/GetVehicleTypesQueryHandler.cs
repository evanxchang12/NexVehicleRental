using MediatR;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.DTOs;
using VehicleRental.Infrastructure.Data;

namespace VehicleRental.Application.Queries.GetVehicleTypes;

public class GetVehicleTypesQueryHandler : IRequestHandler<GetVehicleTypesQuery, IEnumerable<VehicleTypeDto>>
{
    private readonly AppDbContext _context;

    public GetVehicleTypesQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<VehicleTypeDto>> Handle(GetVehicleTypesQuery request, CancellationToken cancellationToken)
    {
        return await _context.VehicleTypes
            .OrderBy(v => v.Id)
            .Select(v => new VehicleTypeDto(v.Id, v.Name, v.Description, v.DailyRate, v.IsAvailable, v.ImageUrl))
            .ToListAsync(cancellationToken);
    }
}
