using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace VehicleRental.Tests.Integration;

public class AccountControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AccountControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ──────────────────────────────────────────
    // 輔助方法
    // ──────────────────────────────────────────

    private async Task<string> GetAntiForgeryTokenAsync(string path)
    {
        var html = await (await _client.GetAsync(path)).Content.ReadAsStringAsync();
        return ExtractAntiForgeryToken(html);
    }

    private async Task<HttpResponseMessage> PostRegisterAsync(
        string fullName, string email, string password, string confirmPassword)
    {
        var token = await GetAntiForgeryTokenAsync("/Account/Register");
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("FullName", fullName),
            new KeyValuePair<string,string>("Email", email),
            new KeyValuePair<string,string>("Password", password),
            new KeyValuePair<string,string>("ConfirmPassword", confirmPassword),
            new KeyValuePair<string,string>("__RequestVerificationToken", token)
        });
        return await _client.PostAsync("/Account/Register", form);
    }

    private async Task<HttpResponseMessage> PostLoginAsync(string email, string password)
    {
        var token = await GetAntiForgeryTokenAsync("/Account/Login");
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("Email", email),
            new KeyValuePair<string,string>("Password", password),
            new KeyValuePair<string,string>("__RequestVerificationToken", token)
        });
        return await _client.PostAsync("/Account/Login", form);
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

    // ──────────────────────────────────────────
    // 測試
    // ──────────────────────────────────────────

    [Fact]
    public async Task Register_Get_Should_Return_200()
    {
        var response = await _client.GetAsync("/Account/Register");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_Post_Should_RedirectToLogin_OnSuccess()
    {
        var response = await PostRegisterAsync("測試用戶A", "usera@example.com", "Test@12345", "Test@12345");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Register_Post_Should_Reject_DuplicateEmail()
    {
        // 先成功註冊一次
        await PostRegisterAsync("既有用戶B", "userb@example.com", "Test@12345", "Test@12345");

        // 第二次使用相同 Email 應失敗
        var response = await PostRegisterAsync("新用戶B", "userb@example.com", "Test@12345", "Test@12345");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("已被使用", html);
    }

    [Fact]
    public async Task Login_Post_Should_SetAuthCookie_OnSuccess()
    {
        // 先註冊
        await PostRegisterAsync("登入測試C", "userc@example.com", "Password@123", "Password@123");

        // 再登入
        var response = await PostLoginAsync("userc@example.com", "Password@123");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Login_Post_Should_ReturnError_OnWrongPassword()
    {
        var response = await PostLoginAsync("nonexist@example.com", "WrongPassword");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("帳號或密碼錯誤", html);
    }

    [Fact]
    public async Task ReservationIndex_Should_RedirectToLogin_WhenUnauthenticated()
    {
        var response = await _client.GetAsync("/Reservation");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
    }
}

