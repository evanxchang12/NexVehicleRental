using System.ComponentModel.DataAnnotations;

namespace VehicleRental.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "請輸入 Email")]
    [EmailAddress(ErrorMessage = "請輸入有效的 Email 格式")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
