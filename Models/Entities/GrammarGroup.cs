using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class GrammarGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<LearningItem> LearningItems { get; set; } = new List<LearningItem>();
}
