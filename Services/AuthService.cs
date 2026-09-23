using English.Data;
using English.Interfaces;
using English.Models;
using English.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

public sealed class AuthService(
    ApplicationDbContext context,
    IPasswordHasher<AspNetUser> passwordHasher) : IAuthService
{
    public async Task<bool> RegisterAsync(
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var trimmedEmail = email.Trim();
        var normalizedEmail = trimmedEmail.ToUpperInvariant();

        if (await context.AspNetUsers.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return false;
        }

        var user = new AspNetUser
        {
            Id = Guid.NewGuid(),
            Email = trimmedEmail,
            NormalizedEmail = normalizedEmail,
            FullName = fullName.Trim(),
            Role = (byte)UserRole.Student,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        context.AspNetUsers.Add(user);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueKeyViolation(exception))
        {
            context.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await context.AspNetUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null || !user.IsActive || string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        PasswordVerificationResult verificationResult;

        try
        {
            verificationResult = passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);
        }
        catch (FormatException)
        {
            return null;
        }

        if (verificationResult == PasswordVerificationResult.Failed ||
            !Enum.IsDefined(typeof(UserRole), user.Role))
        {
            return null;
        }

        return new AuthenticatedUser(
            user.Id,
            user.Email,
            user.FullName,
            (UserRole)user.Role);
    }

    private static bool IsUniqueKeyViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }
        }

        return false;
    }
}
