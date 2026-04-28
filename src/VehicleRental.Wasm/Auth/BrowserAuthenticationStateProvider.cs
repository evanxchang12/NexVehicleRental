using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using VehicleRental.Application.Interfaces;

namespace VehicleRental.Wasm.Auth;

public class BrowserAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string SessionKey = "vehiclerental-session";
    private const string PersistKey = "vehiclerental-auth";
    private const int Pbkdf2Iterations = 100_000;
    private const int HashLength = 32;

    private readonly IJSRuntime _js;
    private readonly IAppDbContext _context;

    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public BrowserAuthenticationStateProvider(IJSRuntime js, IAppDbContext context)
    {
        _js = js;
        _context = context;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser.Identity?.IsAuthenticated == true)
            return new AuthenticationState(_currentUser);

        // Try restore from sessionStorage first, then localStorage
        var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", SessionKey)
                   ?? await _js.InvokeAsync<string?>("localStorage.getItem", PersistKey);

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var session = JsonSerializer.Deserialize<SessionData>(json);
                if (session != null)
                {
                    _currentUser = BuildPrincipal(session.CustomerId, session.FullName, session.Email);
                    return new AuthenticationState(_currentUser);
                }
            }
            catch { /* corrupted storage — fall through */ }
        }

        return new AuthenticationState(_currentUser);
    }

    public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == email);

        if (customer is null || !VerifyPassword(password, customer.PasswordHash))
            return false;

        var session = new SessionData
        {
            CustomerId = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email
        };
        var json = JsonSerializer.Serialize(session);

        await _js.InvokeVoidAsync("sessionStorage.setItem", SessionKey, json);
        if (rememberMe)
            await _js.InvokeVoidAsync("localStorage.setItem", PersistKey, json);

        _currentUser = BuildPrincipal(customer.Id, customer.FullName, customer.Email);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        return true;
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("sessionStorage.removeItem", SessionKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", PersistKey);
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public int? CurrentCustomerId =>
        int.TryParse(_currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public string? CurrentCustomerName => _currentUser.FindFirst(ClaimTypes.Name)?.Value;

    // --- Password helpers ---

    public static string HashPassword(string plainPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plainPassword),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            HashLength);
        return $"PBKDF2:v1:{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }

    public static bool VerifyPassword(string plainPassword, string storedHash)
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
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                HashLength);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }

    private static ClaimsPrincipal BuildPrincipal(int customerId, string fullName, string email)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, customerId.ToString()),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Email, email),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "BrowserAuth"));
    }

    private sealed class SessionData
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
