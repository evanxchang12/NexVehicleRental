using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using VehicleRental.Domain.Entities;
using VehicleRental.Domain.Enums;
using VehicleRental.Infrastructure.Data;
using Xunit;

namespace VehicleRental.Tests.Integration;

public class ReservationControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _unauthClient;
    private readonly TestWebApplicationFactory _factory;

    public ReservationControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _unauthClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Index_Should_RedirectToLogin_WhenUnauthenticated()
    {
        var response = await _unauthClient.GetAsync("/Reservation");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task CalculateCost_Should_Return_CorrectJson()
    {
        // Arrange — seed 車型
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!ctx.VehicleTypes.Any(v => v.Id == 10))
        {
            ctx.VehicleTypes.Add(new VehicleType { Id = 10, Name = "Test", Description = "", DailyRate = 2000m, IsAvailable = true });
            ctx.SaveChanges();
        }

        // Act — CalculateCost 不需要登入（API）
        var response = await _unauthClient.GetAsync(
            "/Reservation/CalculateCost?vehicleTypeId=10&startDate=2026-06-01&endDate=2026-06-04");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("6000", json);   // 2000 × 3 = 6000
        Assert.Contains("\"days\":3", json);
    }

    [Fact]
    public async Task CreateReservation_Command_Should_Detect_TimeConflict()
    {
        // Arrange — 直接測試 Handler（不需 HTTP）
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("ConflictTest-" + Guid.NewGuid())
            .Options;
        using var ctx = new AppDbContext(options);

        ctx.VehicleTypes.Add(new VehicleType { Id = 1, Name = "Car", Description = "", DailyRate = 1500m, IsAvailable = true });
        ctx.Customers.Add(new Customer { Id = 1, FullName = "User", Email = "u@u.com", PasswordHash = "x", CreatedAt = DateTimeOffset.UtcNow });

        ctx.Reservations.Add(new Reservation
        {
            ReservationNumber = "RES-TEST-0001",
            CustomerId = 1,
            VehicleTypeId = 1,
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 5),
            TotalCost = 6000m,
            Status = ReservationStatus.Confirmed,
            CreatedAt = DateTimeOffset.UtcNow
        });
        ctx.SaveChanges();

        var handler = new Application.Commands.CreateReservation.CreateReservationCommandHandler(ctx);

        // Act — 嘗試建立衝突預約 (2026-07-03 ~ 2026-07-07)
        var result = await handler.Handle(
            new Application.Commands.CreateReservation.CreateReservationCommand(1, 1,
                new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 7)),
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("已被預約", result.Error);
    }

    [Fact]
    public async Task CreateReservation_Command_Should_Fail_WhenEndDateBeforeStartDate()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("DateTest-" + Guid.NewGuid())
            .Options;
        using var ctx = new AppDbContext(options);
        ctx.VehicleTypes.Add(new VehicleType { Id = 1, Name = "Car", Description = "", DailyRate = 1500m, IsAvailable = true });
        ctx.SaveChanges();

        var handler = new Application.Commands.CreateReservation.CreateReservationCommandHandler(ctx);

        var result = await handler.Handle(
            new Application.Commands.CreateReservation.CreateReservationCommand(1, 1,
                new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 3)),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("結束日期", result.Error);
    }

    [Fact]
    public async Task CreateReservation_Command_Should_Succeed_ForValidDates()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("SuccessTest-" + Guid.NewGuid())
            .Options;
        using var ctx = new AppDbContext(options);
        ctx.VehicleTypes.Add(new VehicleType { Id = 1, Name = "Car", Description = "", DailyRate = 1800m, IsAvailable = true });
        ctx.Customers.Add(new Customer { Id = 1, FullName = "User", Email = "u@u.com", PasswordHash = "x", CreatedAt = DateTimeOffset.UtcNow });
        ctx.SaveChanges();

        var handler = new Application.Commands.CreateReservation.CreateReservationCommandHandler(ctx);

        var result = await handler.Handle(
            new Application.Commands.CreateReservation.CreateReservationCommand(1, 1,
                new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 4)),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.ReservationId > 0);
        Assert.Equal(1, ctx.Reservations.Count());
        Assert.Equal(5400m, ctx.Reservations.First().TotalCost); // 1800 × 3
    }
}
