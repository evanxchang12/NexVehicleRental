using MediatR;

namespace VehicleRental.Application.Commands.RegisterCustomer;

public record RegisterCustomerCommand(string FullName, string Email, string Password) : IRequest<bool>;
