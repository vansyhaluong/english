using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class Question
{
    public int Id { get; set; }

    public int? ExerciseId { get; set; }

    public int? ToeicPartId { get; set; }

    public int? QuestionGroupId { get; set; }

    public byte Type { get; set; }

    public int DisplayOrder { get; set; }

    public string Prompt { get; set; } = null!;

    public string? Explanation { get; set; }

    public string? Transcript { get; set; }

    public string? Translation { get; set; }

    public virtual ICollection<AcceptedAnswer> AcceptedAnswers { get; set; } = new List<AcceptedAnswer>();

    public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();

    public virtual Exercise? Exercise { get; set; }

    public virtual QuestionGroup? QuestionGroup { get; set; }

    public virtual ToeicPart? ToeicPart { get; set; }
}
