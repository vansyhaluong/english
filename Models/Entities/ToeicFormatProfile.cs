using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class ToeicFormatProfile
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int Version { get; set; }

    public int ListeningSeconds { get; set; }

    public int ReadingSeconds { get; set; }

    public int RulesSchemaVersion { get; set; }

    public string RulesJson { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public virtual ICollection<ToeicTest> ToeicTests { get; set; } = new List<ToeicTest>();
}
