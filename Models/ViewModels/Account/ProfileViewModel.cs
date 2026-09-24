using System.ComponentModel.DataAnnotations;

namespace English.Models.ViewModels.Account;

public sealed class ProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; init; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; init; } = string.Empty;

    [Display(Name = "Vai trò")]
    public string Role { get; init; } = string.Empty;
}

public sealed class UpdateProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;
}
