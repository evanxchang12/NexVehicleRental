using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.DTOs.Results;
using VehicleRental.Application.Interfaces;

namespace VehicleRental.Application.Commands.LoginCustomer;

public class LoginCustomerCommandHandler : IRequestHandler<LoginCustomerCommand, LoginResult>
{
    private const int Iterations = 100_000;
    private const int HashLength = 32;

    private readonly IAppDbContext _context;

    public LoginCustomerCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<LoginResult> Handle(LoginCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == request.Email, cancellationToken);

        if (customer is null)
            return new LoginResult(false, null, null);

        if (!VerifyPassword(request.Password, customer.PasswordHash))
            return new LoginResult(false, null, null);

        return new LoginResult(true, customer.Id, customer.FullName);
    }

    private static bool VerifyPassword(string plainPassword, string storedHash)
    {
        if (!storedHash.StartsWith("PBKDF2:v1:"))
            return false;

        var parts = storedHash.Split(':');
        if (parts.Length != 4)
            return false;

        try
        {
            var salt = Convert.FromHexString(parts[2]);
            var expected = Convert.FromHexString(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(plainPassword),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashLength);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}

