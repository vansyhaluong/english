using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class LearningItem
{
    public int Id { get; set; }

    public byte Kind { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int LevelId { get; set; }

    public int? TopicId { get; set; }

    public int? GrammarGroupId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsVisible { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    public virtual GrammarGroup? GrammarGroup { get; set; }

    public virtual GrammarLesson? GrammarLesson { get; set; }

    public virtual Level Level { get; set; } = null!;

    public virtual ListeningLesson? ListeningLesson { get; set; }

    public virtual ReadingLesson? ReadingLesson { get; set; }

    public virtual Topic? Topic { get; set; }

    public virtual ICollection<UserLearningProgress> UserLearningProgresses { get; set; } = new List<UserLearningProgress>();

    public virtual Vocabulary? Vocabulary { get; set; }

    public virtual WritingTopic? WritingTopic { get; set; }
}
