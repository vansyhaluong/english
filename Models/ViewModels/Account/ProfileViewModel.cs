using System.ComponentModel.DataAnnotations;

namespace English.Models.ViewModels.Account;

public sealed class ProfileViewModel
{
    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.RequiredFullName))]
    [StringLength(200, ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.MaxFullName))]
    [Display(Name = nameof(SharedResource.FullName), ResourceType = typeof(SharedResource))]
    public string FullName { get; init; } = string.Empty;

    [Display(Name = nameof(SharedResource.Email), ResourceType = typeof(SharedResource))]
    public string Email { get; init; } = string.Empty;

    [Display(Name = nameof(SharedResource.Role), ResourceType = typeof(SharedResource))]
    public string Role { get; init; } = string.Empty;
}

public sealed class UpdateProfileViewModel
{
    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.RequiredFullName))]
    [StringLength(200, ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.MaxFullName))]
    [Display(Name = nameof(SharedResource.FullName), ResourceType = typeof(SharedResource))]
    public string FullName { get; set; } = string.Empty;
}
