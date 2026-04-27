using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehicleRental.Application.Commands.CancelReservation;
using VehicleRental.Application.Commands.CreateReservation;
using VehicleRental.Application.Queries.CalculateRentalCost;
using VehicleRental.Application.Queries.GetMyReservations;
using VehicleRental.Application.Queries.GetVehicleTypes;
using VehicleRental.Infrastructure.Data;
using VehicleRental.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace VehicleRental.Web.Controllers;

[Authorize]
public class ReservationController : Controller
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;

    public ReservationController(IMediator mediator, AppDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vehicleTypes = await _mediator.Send(new GetVehicleTypesQuery());
        return View(new VehicleTypeListViewModel { VehicleTypes = vehicleTypes });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int vehicleTypeId)
    {
        var vehicleType = await _context.VehicleTypes.FindAsync(vehicleTypeId);
        if (vehicleType is null)
            return RedirectToAction(nameof(Index));

        var model = new CreateReservationViewModel
        {
            VehicleTypeId = vehicleType.Id,
            VehicleTypeName = vehicleType.Name,
            DailyRate = vehicleType.DailyRate,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReservationViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _mediator.Send(new CreateReservationCommand(
            customerId,
            model.VehicleTypeId,
            model.StartDate,
            model.EndDate));

        if (!result.Success)
        {
            // Reload vehicle type name for display
            var vehicleType = await _context.VehicleTypes.FindAsync(model.VehicleTypeId);
            model.VehicleTypeName = vehicleType?.Name ?? string.Empty;
            model.DailyRate = vehicleType?.DailyRate ?? 0;
            ModelState.AddModelError(string.Empty, result.Error ?? "預約建立失敗，請重試");
            return View(model);
        }

        return RedirectToAction(nameof(Confirmation), new { id = result.ReservationId });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.VehicleType)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
            return RedirectToAction(nameof(Index));

        return View(reservation);
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> CalculateCost(int vehicleTypeId, string startDate, string endDate)
    {
        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            return Json(new { error = "日期格式錯誤" });

        var result = await _mediator.Send(new CalculateRentalCostQuery(vehicleTypeId, start, end));

        if (result is null)
            return Json(new { error = "車型不存在" });

        return Json(new { totalCost = result.TotalCost, days = result.Days, dailyRate = result.DailyRate });
    }

    [HttpGet]
    public async Task<IActionResult> My()
    {
        var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var reservations = await _mediator.Send(new GetMyReservationsQuery(customerId));
        return View(new MyReservationsViewModel { Reservations = reservations });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _mediator.Send(new CancelReservationCommand(id, customerId));

        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "預約已成功取消" : "無法取消此預約（可能已開始或不存在）";

        return RedirectToAction(nameof(My));
    }
}
