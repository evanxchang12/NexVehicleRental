using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using VehicleRental.Application.Interfaces;
using VehicleRental.Domain.Entities;
using VehicleRental.Domain.Enums;

namespace VehicleRental.Wasm.Services;

public class LocalStoragePersistenceService : IPersistenceService
{
    private const string StorageKey = "vehiclerental-data";

    private readonly IAppDbContext _context;
    private readonly IJSRuntime _js;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LocalStoragePersistenceService(IAppDbContext context, IJSRuntime js)
    {
        _context = context;
        _js = js;
    }

    public async Task SaveAsync()
    {
        var customers = await _context.Customers.ToListAsync();
        var vehicleTypes = await _context.VehicleTypes.ToListAsync();
        var reservations = await _context.Reservations.ToListAsync();

        var data = new PersistedData
        {
            Customers = customers.Select(c => new PersistedCustomer
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                PasswordHash = c.PasswordHash,
                CreatedAt = c.CreatedAt
            }).ToList(),
            VehicleTypes = vehicleTypes.Select(v => new PersistedVehicleType
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                DailyRate = v.DailyRate,
                IsAvailable = v.IsAvailable,
                ImageUrl = v.ImageUrl
            }).ToList(),
            Reservations = reservations.Select(r => new PersistedReservation
            {
                Id = r.Id,
                ReservationNumber = r.ReservationNumber,
                CustomerId = r.CustomerId,
                VehicleTypeId = r.VehicleTypeId,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                TotalCost = r.TotalCost,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToList()
        };

        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task RestoreAsync()
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrEmpty(json))
            return;

        PersistedData? data;
        try
        {
            data = JsonSerializer.Deserialize<PersistedData>(json, _jsonOptions);
        }
        catch
        {
            return;
        }

        if (data is null)
            return;

        foreach (var c in data.Customers)
        {
            if (!await _context.Customers.AnyAsync(x => x.Id == c.Id))
            {
                _context.Customers.Add(new Customer
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    PasswordHash = c.PasswordHash,
                    CreatedAt = c.CreatedAt
                });
            }
        }

        foreach (var v in data.VehicleTypes)
        {
            if (!await _context.VehicleTypes.AnyAsync(x => x.Id == v.Id))
            {
                _context.VehicleTypes.Add(new VehicleType
                {
                    Id = v.Id,
                    Name = v.Name,
                    Description = v.Description ?? string.Empty,
                    DailyRate = v.DailyRate,
                    IsAvailable = v.IsAvailable,
                    ImageUrl = v.ImageUrl
                });
            }
        }

        foreach (var r in data.Reservations)
        {
            if (!await _context.Reservations.AnyAsync(x => x.Id == r.Id))
            {
                _context.Reservations.Add(new Reservation
                {
                    Id = r.Id,
                    ReservationNumber = r.ReservationNumber,
                    CustomerId = r.CustomerId,
                    VehicleTypeId = r.VehicleTypeId,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    TotalCost = r.TotalCost,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    // Internal DTOs for serialization (avoids EF navigation property cycles)
    private sealed class PersistedData
    {
        public List<PersistedCustomer> Customers { get; set; } = [];
        public List<PersistedVehicleType> VehicleTypes { get; set; } = [];
        public List<PersistedReservation> Reservations { get; set; } = [];
    }

    private sealed class PersistedCustomer
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }

    private sealed class PersistedVehicleType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DailyRate { get; set; }
        public bool IsAvailable { get; set; }
        public string? ImageUrl { get; set; }
    }

    private sealed class PersistedReservation
    {
        public int Id { get; set; }
        public string ReservationNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public int VehicleTypeId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TotalCost { get; set; }
        public ReservationStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
