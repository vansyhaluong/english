using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class UserLearningProgress
{
    public Guid UserId { get; set; }

    public int LearningItemId { get; set; }

    public bool IsLearned { get; set; }

    public DateTimeOffset? LearnedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public virtual LearningItem LearningItem { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
