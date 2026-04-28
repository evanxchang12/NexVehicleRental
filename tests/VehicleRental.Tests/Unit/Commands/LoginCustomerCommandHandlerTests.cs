using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.Commands.LoginCustomer;
using VehicleRental.Application.Commands.RegisterCustomer;
using VehicleRental.Infrastructure.Data;
using Xunit;

namespace VehicleRental.Tests.Unit.Commands;

public class LoginCustomerCommandHandlerTests
{
    private static async Task<AppDbContext> CreateDbWithUserAsync(
        string email = "user@example.com", string password = "Pa$$w0rd!")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        // Register a user first (uses PBKDF2)
        var registerHandler = new RegisterCustomerCommandHandler(db);
        await registerHandler.Handle(
            new RegisterCustomerCommand("Test User", email, password),
            CancellationToken.None);

        return db;
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccess()
    {
        using var db = await CreateDbWithUserAsync();
        var handler = new LoginCustomerCommandHandler(db);

        var result = await handler.Handle(
            new LoginCustomerCommand("user@example.com", "Pa$$w0rd!"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.CustomerId);
        Assert.Equal("Test User", result.FullName);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailed()
    {
        using var db = await CreateDbWithUserAsync();
        var handler = new LoginCustomerCommandHandler(db);

        var result = await handler.Handle(
            new LoginCustomerCommand("user@example.com", "WrongPassword"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Null(result.CustomerId);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsFailed()
    {
        using var db = await CreateDbWithUserAsync();
        var handler = new LoginCustomerCommandHandler(db);

        var result = await handler.Handle(
            new LoginCustomerCommand("nobody@example.com", "Pa$$w0rd!"),
            CancellationToken.None);

        Assert.False(result.Success);
    }
}
