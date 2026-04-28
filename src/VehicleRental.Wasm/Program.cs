using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VehicleRental.Application.Commands.RegisterCustomer;
using VehicleRental.Application.Interfaces;
using VehicleRental.Infrastructure;
using VehicleRental.Wasm;
using VehicleRental.Wasm.Auth;
using VehicleRental.Wasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Application Layer — MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterCustomerCommand).Assembly));

// Infrastructure Layer — EF Core InMemory + DbInitializer
builder.Services.AddInfrastructureWasm();

// Wasm-specific persistence (needs IJSRuntime)
builder.Services.AddSingleton<IPersistenceService, LocalStoragePersistenceService>();

// Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<BrowserAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<BrowserAuthenticationStateProvider>());

var app = builder.Build();

// Initialize DB with seed data, then restore persisted user/reservation data
var initializer = app.Services.GetRequiredService<IDbInitializer>();
await initializer.InitializeAsync();

var persistence = app.Services.GetRequiredService<IPersistenceService>();
await persistence.RestoreAsync();

await app.RunAsync();


