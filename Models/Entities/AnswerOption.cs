using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class AnswerOption
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public int DisplayOrder { get; set; }

    public string Text { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public virtual Question Question { get; set; } = null!;
}
