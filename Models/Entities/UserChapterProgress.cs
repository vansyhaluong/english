using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class UserChapterProgress
{
    public Guid UserId { get; set; }

    public int ChapterId { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public virtual BookChapter Chapter { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
