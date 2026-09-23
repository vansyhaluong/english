using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class BookChapter
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public int DisplayOrder { get; set; }

    public string Title { get; set; } = null!;

    public string ContentEn { get; set; } = null!;

    public string? TranslationVi { get; set; }

    public string? VocabularyNotes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsVisible { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Book Book { get; set; } = null!;

    public virtual ICollection<BookReadingPosition> BookReadingPositions { get; set; } = new List<BookReadingPosition>();

    public virtual ICollection<UserChapterProgress> UserChapterProgresses { get; set; } = new List<UserChapterProgress>();
}
