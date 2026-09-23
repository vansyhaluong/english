using English.Models;

namespace English.Interfaces;

public sealed record AuthenticatedUser(Guid Id, string Email, string FullName, UserRole Role);

public interface IAuthService
{
    Task<bool> RegisterAsync(
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedUser?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
