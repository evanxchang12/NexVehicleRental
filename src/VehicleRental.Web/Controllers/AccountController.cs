using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehicleRental.Application.Commands.LoginCustomer;
using VehicleRental.Application.Commands.RegisterCustomer;
using VehicleRental.Web.Models.ViewModels;

namespace VehicleRental.Web.Controllers;

public class AccountController : Controller
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var success = await _mediator.Send(
            new RegisterCustomerCommand(model.FullName, model.Email, model.Password));

        if (!success)
        {
            ModelState.AddModelError("Email", "此 Email 已被使用，請使用其他 Email 或直接登入");
            return View(model);
        }

        TempData["SuccessMessage"] = "註冊成功！請登入以繼續";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _mediator.Send(
            new LoginCustomerCommand(model.Email, model.Password));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, "帳號或密碼錯誤，請重新輸入");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.CustomerId.ToString()!),
            new(ClaimTypes.Name, result.FullName!),
            new(ClaimTypes.Email, model.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Reservation");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
