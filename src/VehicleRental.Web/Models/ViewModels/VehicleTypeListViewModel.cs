using VehicleRental.Application.DTOs;

namespace VehicleRental.Web.Models.ViewModels;

public class VehicleTypeListViewModel
{
    public IEnumerable<VehicleTypeDto> VehicleTypes { get; set; } = [];
}
