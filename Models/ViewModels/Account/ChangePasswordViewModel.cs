using System.ComponentModel.DataAnnotations;

namespace English.Models.ViewModels.Account;

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.RequiredCurrentPassword))]
    [DataType(DataType.Password)]
    [Display(Name = nameof(SharedResource.CurrentPassword), ResourceType = typeof(SharedResource))]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.RequiredNewPassword))]
    [MinLength(6, ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.MinNewPassword))]
    [DataType(DataType.Password)]
    [Display(Name = nameof(SharedResource.NewPassword), ResourceType = typeof(SharedResource))]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.RequiredConfirmNewPassword))]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.PasswordMismatch))]
    [Display(Name = nameof(SharedResource.ConfirmNewPassword), ResourceType = typeof(SharedResource))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
