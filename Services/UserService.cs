using English.Data;
using English.Interfaces;
using English.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

public sealed class UserService(
    ApplicationDbContext context,
    IPasswordHasher<AspNetUser> passwordHasher) : IUserService
{
    public Task<UserProfile?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.AspNetUsers
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserProfile(
                user.Id,
                user.Email,
                user.FullName,
                user.Role))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateProfileAsync(
        Guid userId,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        var user = await context.AspNetUsers.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.FullName = fullName.Trim();
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await context.AspNetUsers.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return ChangePasswordResult.UserNotFound;
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return ChangePasswordResult.InvalidCurrentPassword;
        }

        PasswordVerificationResult verificationResult;
        try
        {
            verificationResult = passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                currentPassword);
        }
        catch (FormatException)
        {
            return ChangePasswordResult.InvalidCurrentPassword;
        }

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return ChangePasswordResult.InvalidCurrentPassword;
        }

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        await context.SaveChangesAsync(cancellationToken);
        return ChangePasswordResult.Success;
    }

    public async Task<IReadOnlyList<AdminUserSummary>> GetUsersAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = context.AspNetUsers.AsNoTracking();
        var searchTerm = search?.Trim();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(user =>
                user.FullName.Contains(searchTerm) ||
                user.Email.Contains(searchTerm));
        }

        return await query
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Email)
            .Select(user => new AdminUserSummary(
                user.Id,
                user.Email,
                user.FullName,
                user.Role,
                user.IsActive))
            .ToListAsync(cancellationToken);
    }

    public Task<AdminUserDetails?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.AspNetUsers
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AdminUserDetails(
                user.Id,
                user.Email,
                user.FullName,
                user.Role,
                user.IsActive,
                user.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await context.AspNetUsers.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
