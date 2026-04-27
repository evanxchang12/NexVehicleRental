using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.Commands.RegisterCustomer;
using VehicleRental.Application.Commands.LoginCustomer;
using VehicleRental.Infrastructure.Data;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace VehicleRental.Tests.Unit;

public class CalculateRentalCostQueryHandlerTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("CostTest-" + Guid.NewGuid())
            .Options;
        var ctx = new AppDbContext(options);

        ctx.VehicleTypes.Add(new Domain.Entities.VehicleType
        {
            Id = 1,
            Name = "Test Car",
            Description = "Test",
            DailyRate = 1800m,
            IsAvailable = true
        });
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task Handle_Should_Return_CorrectCost_ForThreeDays()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var handler = new Application.Queries.CalculateRentalCost.CalculateRentalCostQueryHandler(ctx);
        var query = new Application.Queries.CalculateRentalCost.CalculateRentalCostQuery(
            1,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 4));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Days);
        Assert.Equal(1800m, result.DailyRate);
        Assert.Equal(5400m, result.TotalCost);
    }

    [Fact]
    public async Task Handle_Should_Return_ZeroCost_WhenSameDayStartEnd()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var handler = new Application.Queries.CalculateRentalCost.CalculateRentalCostQueryHandler(ctx);
        var query = new Application.Queries.CalculateRentalCost.CalculateRentalCostQuery(
            1,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 1));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Days);
        Assert.Equal(0m, result.TotalCost);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_ForUnknownVehicleType()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var handler = new Application.Queries.CalculateRentalCost.CalculateRentalCostQueryHandler(ctx);
        var query = new Application.Queries.CalculateRentalCost.CalculateRentalCostQuery(
            999,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 4));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
