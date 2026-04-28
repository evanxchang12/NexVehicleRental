using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.Interfaces;
using VehicleRental.Domain.Entities;

namespace VehicleRental.Application.Commands.RegisterCustomer;

public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, bool>
{
    private const int Iterations = 100_000;
    private const int HashLength = 32;

    private readonly IAppDbContext _context;
    private readonly IPersistenceService? _persistence;

    public RegisterCustomerCommandHandler(IAppDbContext context, IPersistenceService? persistence = null)
    {
        _context = context;
        _persistence = persistence;
    }

    public async Task<bool> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await _context.Customers
            .AnyAsync(c => c.Email == request.Email, cancellationToken);

        if (emailExists)
            return false;

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(request.Password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashLength);

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = $"PBKDF2:v1:{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        if (_persistence is not null)
            await _persistence.SaveAsync();

        return true;
    }
}

