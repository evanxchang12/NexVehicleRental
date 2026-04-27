using System.ComponentModel.DataAnnotations;

namespace VehicleRental.Web.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "請輸入姓名")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "姓名需為 2~100 字元")]
    [Display(Name = "姓名")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入 Email")]
    [EmailAddress(ErrorMessage = "請輸入有效的 Email 格式")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼需至少 6 字元")]
    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "請確認密碼")]
    [Compare("Password", ErrorMessage = "密碼與確認密碼不符")]
    [DataType(DataType.Password)]
    [Display(Name = "確認密碼")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
