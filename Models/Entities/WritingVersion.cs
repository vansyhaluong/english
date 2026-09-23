using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class WritingVersion
{
    public Guid Id { get; set; }

    public Guid UserWritingId { get; set; }

    public int VersionNumber { get; set; }

    public string Text { get; set; } = null!;

    public byte[] TextHash { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public virtual UserWriting UserWriting { get; set; } = null!;

    public virtual ICollection<WritingEvaluation> WritingEvaluations { get; set; } = new List<WritingEvaluation>();
}
