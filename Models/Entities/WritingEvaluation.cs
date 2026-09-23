using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class WritingEvaluation
{
    public Guid Id { get; set; }

    public Guid UserWritingId { get; set; }

    public Guid VersionId { get; set; }

    public int RunNumber { get; set; }

    public byte Status { get; set; }

    public decimal? TaskRelevanceScore { get; set; }

    public decimal? GrammarScore { get; set; }

    public decimal? VocabularyScore { get; set; }

    public decimal? OrganizationCohesionScore { get; set; }

    public decimal? OverallScore { get; set; }

    public string? FeedbackJson { get; set; }

    public string Provider { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string PromptVersion { get; set; } = null!;

    public string PromptSnapshot { get; set; } = null!;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ErrorCode { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual UserWriting UserWriting { get; set; } = null!;

    public virtual WritingVersion WritingVersion { get; set; } = null!;
}
