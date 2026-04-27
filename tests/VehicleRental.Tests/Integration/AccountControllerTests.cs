using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using VehicleRental.Infrastructure.Data;
using Xunit;

namespace VehicleRental.Tests.Integration;

public class AccountControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AccountControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Register_Get_Should_Return_200()
    {
        var response = await _client.GetAsync("/Account/Register");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_Post_Should_RedirectToLogin_OnSuccess()
    {
        // Arrange — 取得 AntiForgery token
        var getResp = await _client.GetAsync("/Account/Register");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("FullName", "測試用戶"),
            new KeyValuePair<string,string>("Email", "test@example.com"),
            new KeyValuePair<string,string>("Password", "Test@12345"),
            new KeyValuePair<string,string>("ConfirmPassword", "Test@12345"),
            new KeyValuePair<string,string>("__RequestVerificationToken", token)
        });

        // Act
        var response = await _client.PostAsync("/Account/Register", form);

        // Assert — 成功後重導向登入頁
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Register_Post_Should_Reject_DuplicateEmail()
    {
        // Arrange — 先建立一個帳號
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        ctx.Customers.Add(new Domain.Entities.Customer
        {
            FullName = "既有用戶",
            Email = "duplicate@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTimeOffset.UtcNow
        });
        ctx.SaveChanges();

        var getResp = await _client.GetAsync("/Account/Register");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("FullName", "新用戶"),
            new KeyValuePair<string,string>("Email", "duplicate@example.com"),
            new KeyValuePair<string,string>("Password", "Test@12345"),
            new KeyValuePair<string,string>("ConfirmPassword", "Test@12345"),
            new KeyValuePair<string,string>("__RequestVerificationToken", token)
        });

        // Act
        var response = await _client.PostAsync("/Account/Register", form);

        // Assert — 回傳 200（重新顯示表單）
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseHtml = await response.Content.ReadAsStringAsync();
        Assert.Contains("已被使用", responseHtml);
    }

    [Fact]
    public async Task Login_Post_Should_SetAuthCookie_OnSuccess()
    {
        // Arrange — 先用 handler 建立帳號
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Domain.Entities.Customer>();
        var customer = new Domain.Entities.Customer
        {
            FullName = "登入測試",
            Email = "login@example.com",
            CreatedAt = DateTimeOffset.UtcNow
        };
        customer.PasswordHash = hasher.HashPassword(customer, "Password@123");
        ctx.Customers.Add(customer);
        ctx.SaveChanges();

        var getResp = await _client.GetAsync("/Account/Login");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("Email", "login@example.com"),
            new KeyValuePair<string,string>("Password", "Password@123"),
            new KeyValuePair<string,string>("__RequestVerificationToken", token)
        });

        // Act
        var response = await _client.PostAsync("/Account/Login", form);

        // Assert — 成功後重導向，且包含 Set-Cookie
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Login_Post_Should_ReturnError_OnWrongPassword()
    {
        var getResp = await _client.GetAsync("/Account/Login");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("Email", "wrong@example.com"),
            new KeyValuePair<string,string>("Password", "WrongPassword"),
            new KeyValuePair<string,string>("__RequestVerificationToken", token)
        });

        var response = await _client.PostAsync("/Account/Login", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseHtml = await response.Content.ReadAsStringAsync();
        Assert.Contains("帳號或密碼錯誤", responseHtml);
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        const string tokenName = "__RequestVerificationToken";
        var start = html.IndexOf($"name=\"{tokenName}\"", StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        var valueStart = html.IndexOf("value=\"", start, StringComparison.Ordinal) + 7;
        var valueEnd = html.IndexOf("\"", valueStart, StringComparison.Ordinal);
        return html[valueStart..valueEnd];
    }
}
