using VehicleRental.Application.DTOs;

namespace VehicleRental.Web.Models.ViewModels;

public class MyReservationsViewModel
{
    public IEnumerable<ReservationSummaryDto> Reservations { get; set; } = [];
}
