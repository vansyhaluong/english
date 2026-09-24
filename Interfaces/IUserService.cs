namespace English.Interfaces;

public sealed record UserProfile(Guid Id, string Email, string FullName, byte Role);

public sealed record AdminUserSummary(
    Guid Id,
    string Email,
    string FullName,
    byte Role,
    bool IsActive);

public sealed record AdminUserDetails(
    Guid Id,
    string Email,
    string FullName,
    byte Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public enum ChangePasswordResult
{
    Success,
    UserNotFound,
    InvalidCurrentPassword
}

public interface IUserService
{
    Task<UserProfile?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateProfileAsync(
        Guid userId,
        string fullName,
        CancellationToken cancellationToken = default);

    Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUserSummary>> GetUsersAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetails?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
