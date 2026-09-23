using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class Attempt
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int? ExerciseId { get; set; }

    public int? ToeicTestId { get; set; }

    public byte Mode { get; set; }

    public byte Scope { get; set; }

    public byte? SelectedPart { get; set; }

    public byte Status { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? SubmittedAtUtc { get; set; }

    public DateTimeOffset? EndedAtUtc { get; set; }

    public int TotalCount { get; set; }

    public int? CorrectCount { get; set; }

    public int? WrongCount { get; set; }

    public int? BlankCount { get; set; }

    public decimal? ScorePercent { get; set; }

    public byte[]? TokenHash { get; set; }

    public DateTimeOffset LastActivityAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<AttemptQuestion> AttemptQuestions { get; set; } = new List<AttemptQuestion>();

    public virtual ICollection<AttemptSection> AttemptSections { get; set; } = new List<AttemptSection>();

    public virtual AttemptSnapshot? AttemptSnapshot { get; set; }

    public virtual Exercise? Exercise { get; set; }

    public virtual ToeicTest? ToeicTest { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}
