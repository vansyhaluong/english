using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class GrammarLesson
{
    public int LearningItemId { get; set; }

    public string Formula { get; set; } = null!;

    public string Usage { get; set; } = null!;

    public string Examples { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual LearningItem LearningItem { get; set; } = null!;
}
