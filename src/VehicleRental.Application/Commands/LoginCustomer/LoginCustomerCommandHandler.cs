using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.DTOs.Results;
using VehicleRental.Domain.Entities;
using VehicleRental.Infrastructure.Data;

namespace VehicleRental.Application.Commands.LoginCustomer;

public class LoginCustomerCommandHandler : IRequestHandler<LoginCustomerCommand, LoginResult>
{
    private readonly AppDbContext _context;

    public LoginCustomerCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LoginResult> Handle(LoginCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == request.Email, cancellationToken);

        if (customer is null)
            return new LoginResult(false, null, null);

        var hasher = new PasswordHasher<Customer>();
        var result = hasher.VerifyHashedPassword(customer, customer.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return new LoginResult(false, null, null);

        return new LoginResult(true, customer.Id, customer.FullName);
    }
}
