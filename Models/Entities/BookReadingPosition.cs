using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class BookReadingPosition
{
    public Guid UserId { get; set; }

    public int BookId { get; set; }

    public int LastChapterId { get; set; }

    public DateTimeOffset LastOpenedAt { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual BookChapter BookChapter { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
