using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class Book
{
    public int Id { get; set; }

    public int TopicId { get; set; }

    public string Title { get; set; } = null!;

    public string Author { get; set; } = null!;

    public string? Description { get; set; }

    public string? CoverUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsVisible { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<BookChapter> BookChapters { get; set; } = new List<BookChapter>();

    public virtual ICollection<BookReadingPosition> BookReadingPositions { get; set; } = new List<BookReadingPosition>();

    public virtual Topic Topic { get; set; } = null!;
}
