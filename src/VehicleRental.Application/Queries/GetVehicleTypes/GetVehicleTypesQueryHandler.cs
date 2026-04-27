using MediatR;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.DTOs;
using VehicleRental.Application.Interfaces;

namespace VehicleRental.Application.Queries.GetVehicleTypes;

public class GetVehicleTypesQueryHandler : IRequestHandler<GetVehicleTypesQuery, IEnumerable<VehicleTypeDto>>
{
    private readonly IAppDbContext _context;

    public GetVehicleTypesQueryHandler(IAppDbContext context)
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
