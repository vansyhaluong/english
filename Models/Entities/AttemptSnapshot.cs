using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class AttemptSnapshot
{
    public Guid AttemptId { get; set; }

    public int SchemaVersion { get; set; }

    public string HeaderJson { get; set; } = null!;

    public string GroupStimulusJson { get; set; } = null!;

    public string? FormatSnapshotJson { get; set; }

    public virtual Attempt Attempt { get; set; } = null!;
}
