using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Application.Interfaces;
using VehicleRental.Domain.Entities;

namespace VehicleRental.Application.Commands.RegisterCustomer;

public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, bool>
{
    private readonly IAppDbContext _context;

    public RegisterCustomerCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await _context.Customers
            .AnyAsync(c => c.Email == request.Email, cancellationToken);

        if (emailExists)
            return false;

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var hasher = new PasswordHasher<Customer>();
        customer.PasswordHash = hasher.HashPassword(customer, request.Password);

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
