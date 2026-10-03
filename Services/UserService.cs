using System.Data.Common;
using English.Data;
using English.Interfaces;
using English.Models;
using English.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

public sealed class UserService(
    ApplicationDbContext context,
    IPasswordHasher<AspNetUser> passwordHasher,
    IFileStorage fileStorage) : IUserService
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
                user.Role,
                user.AvatarFileId))
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

    public async Task<AvatarUploadResult> UpdateAvatarAsync(
        Guid userId,
        AvatarUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        ArgumentNullException.ThrowIfNull(upload.Content);

        if (upload.SizeBytes <= 0 || upload.SizeBytes > AvatarUploadLimits.MaxSizeBytes)
        {
            return AvatarUploadResult.InvalidSize;
        }

        var user = await context.AspNetUsers.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return AvatarUploadResult.UserNotFound;
        }

        await using var bufferedContent = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var bytesRead = await upload.Content.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            if (bufferedContent.Length + bytesRead > AvatarUploadLimits.MaxSizeBytes)
            {
                return AvatarUploadResult.InvalidSize;
            }

            await bufferedContent.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        if (bufferedContent.Length == 0 ||
            !TryIdentifyAvatar(bufferedContent.GetBuffer().AsSpan(0, (int)bufferedContent.Length), out var avatarType))
        {
            return AvatarUploadResult.InvalidContent;
        }

        bufferedContent.Position = 0;
        string storageKey;
        try
        {
            storageKey = await fileStorage.SaveAsync(
                bufferedContent,
                avatarType.Extension,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return AvatarUploadResult.StorageFailure;
        }

        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var storedFile = new StoredFile
            {
                StorageKey = storageKey,
                Kind = (byte)StoredFileKind.Avatar,
                ContentType = avatarType.ContentType,
                SizeBytes = bufferedContent.Length,
                OriginalName = NormalizeOriginalName(upload.OriginalName, avatarType.Extension),
                CreatedAt = DateTimeOffset.UtcNow,
                UploadedByUserId = userId
            };

            context.StoredFiles.Add(storedFile);
            await context.SaveChangesAsync(cancellationToken);

            user.AvatarFileId = storedFile.Id;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AvatarUploadResult.Success;
        }
        catch (OperationCanceledException)
        {
            await TryDeleteStoredFileAsync(storageKey);
            throw;
        }
        catch (Exception exception) when (
            exception is DbUpdateException or DbException or InvalidOperationException)
        {
            context.ChangeTracker.Clear();
            await TryDeleteStoredFileAsync(storageKey);
            return AvatarUploadResult.StorageFailure;
        }
        catch
        {
            context.ChangeTracker.Clear();
            await TryDeleteStoredFileAsync(storageKey);
            throw;
        }
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

    private static bool TryIdentifyAvatar(
        ReadOnlySpan<byte> content,
        out AvatarContentType avatarType)
    {
        if (AvatarImageContent.TryDecode(content, out var contentType, out var extension))
        {
            avatarType = new AvatarContentType(contentType, extension);
            return true;
        }

        avatarType = default;
        return false;
    }

    private static string NormalizeOriginalName(string? originalName, string extension)
    {
        var normalized = Path.GetFileName((originalName ?? string.Empty).Replace('\\', '/'))
            .Trim();
        normalized = string.Concat(normalized.Where(character => !char.IsControl(character)));

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = $"avatar{extension}";
        }

        return normalized.Length <= 255 ? normalized : normalized[..255];
    }

    private async Task TryDeleteStoredFileAsync(string storageKey)
    {
        try
        {
            await fileStorage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
        }
    }

    private readonly record struct AvatarContentType(string ContentType, string Extension);
}
