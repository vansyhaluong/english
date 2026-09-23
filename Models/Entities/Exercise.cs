using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class Exercise
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public byte OwnerKind { get; set; }

    public int? LearningItemId { get; set; }

    public int? VocabularyTopicId { get; set; }

    public int? LevelId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsVisible { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();

    public virtual LearningItem? LearningItem { get; set; }

    public virtual Level? Level { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual Topic? VocabularyTopic { get; set; }
}
