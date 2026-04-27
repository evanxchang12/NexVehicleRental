using MediatR;
using VehicleRental.Application.DTOs.Results;

namespace VehicleRental.Application.Commands.LoginCustomer;

public record LoginCustomerCommand(string Email, string Password) : IRequest<LoginResult>;
