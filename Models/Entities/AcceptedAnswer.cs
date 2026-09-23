using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class AcceptedAnswer
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string Text { get; set; } = null!;

    public string NormalizedText { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
