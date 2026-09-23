using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class AttemptQuestion
{
    public Guid Id { get; set; }

    public Guid AttemptId { get; set; }

    public int? SourceQuestionId { get; set; }

    public byte? SectionKind { get; set; }

    public byte? PartNumber { get; set; }

    public int DisplayOrder { get; set; }

    public string SnapshotJson { get; set; } = null!;

    public virtual Attempt Attempt { get; set; } = null!;

    public virtual AttemptAnswer? AttemptAnswer { get; set; }
}
