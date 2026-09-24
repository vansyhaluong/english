namespace English.Models.ViewModels.AdminUsers;

public sealed class AdminUsersIndexViewModel
{
    public string? Search { get; init; }

    public IReadOnlyList<AdminUserListItemViewModel> Users { get; init; } = [];
}

public sealed record AdminUserListItemViewModel(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive);

public sealed record AdminUserDetailsViewModel(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);
