using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class AttemptAnswer
{
    public Guid AttemptQuestionId { get; set; }

    public string? SelectedOptionSnapshotKey { get; set; }

    public string? Text { get; set; }

    public long Sequence { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual AttemptQuestion AttemptQuestion { get; set; } = null!;
}
