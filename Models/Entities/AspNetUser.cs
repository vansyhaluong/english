using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class AspNetUser
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string NormalizedEmail { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string FullName { get; set; } = null!;

    public byte Role { get; set; }

    public bool IsActive { get; set; }

    public int? SelectedLevelId { get; set; }

    public int? AvatarFileId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public virtual ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();

    public virtual StoredFile? AvatarFile { get; set; }

    public virtual ICollection<BookReadingPosition> BookReadingPositions { get; set; } = new List<BookReadingPosition>();

    public virtual Level? SelectedLevel { get; set; }

    public virtual ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();

    public virtual ICollection<UserChapterProgress> UserChapterProgresses { get; set; } = new List<UserChapterProgress>();

    public virtual ICollection<UserLearningProgress> UserLearningProgresses { get; set; } = new List<UserLearningProgress>();

    public virtual ICollection<UserWriting> UserWritings { get; set; } = new List<UserWriting>();
}
