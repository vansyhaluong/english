using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class ToeicTest
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int FormatProfileId { get; set; }

    public byte PublicationStatus { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsVisible { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();

    public virtual ToeicFormatProfile FormatProfile { get; set; } = null!;

    public virtual ICollection<ToeicSection> ToeicSections { get; set; } = new List<ToeicSection>();
}
