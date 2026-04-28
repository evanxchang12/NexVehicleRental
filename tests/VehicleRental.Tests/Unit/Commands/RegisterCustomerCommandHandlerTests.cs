using Microsoft.EntityFrameworkCore;
using Moq;
using VehicleRental.Application.Commands.RegisterCustomer;
using VehicleRental.Application.Interfaces;
using VehicleRental.Infrastructure.Data;
using Xunit;

namespace VehicleRental.Tests.Unit.Commands;

public class RegisterCustomerCommandHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_NewEmail_ReturnsTrue_AndHashesPassword()
    {
        using var db = CreateDb();
        var handler = new RegisterCustomerCommandHandler(db);

        var result = await handler.Handle(
            new RegisterCustomerCommand("Alice", "alice@example.com", "Pa$$w0rd!"),
            CancellationToken.None);

        Assert.True(result);
        var customer = await db.Customers.FirstAsync();
        Assert.Equal("alice@example.com", customer.Email);
        Assert.StartsWith("PBKDF2:v1:", customer.PasswordHash);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFalse()
    {
        using var db = CreateDb();
        var handler = new RegisterCustomerCommandHandler(db);
        var cmd = new RegisterCustomerCommand("Alice", "alice@example.com", "Pa$$w0rd!");

        await handler.Handle(cmd, CancellationToken.None);
        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_SavesPersistence_WhenProvided()
    {
        using var db = CreateDb();
        var mockPersist = new Mock<IPersistenceService>();
        var handler = new RegisterCustomerCommandHandler(db, mockPersist.Object);

        await handler.Handle(
            new RegisterCustomerCommand("Bob", "bob@example.com", "secret"),
            CancellationToken.None);

        mockPersist.Verify(p => p.SaveAsync(), Times.Once);
    }
}
