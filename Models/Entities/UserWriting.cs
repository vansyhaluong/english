using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class UserWriting
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int WritingTopicId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;

    public virtual ICollection<WritingEvaluation> WritingEvaluations { get; set; } = new List<WritingEvaluation>();

    public virtual WritingTopic WritingTopic { get; set; } = null!;

    public virtual ICollection<WritingVersion> WritingVersions { get; set; } = new List<WritingVersion>();
}
