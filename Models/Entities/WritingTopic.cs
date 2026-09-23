using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class WritingTopic
{
    public int LearningItemId { get; set; }

    public string Prompt { get; set; } = null!;

    public string? SuggestedVocabulary { get; set; }

    public virtual LearningItem LearningItem { get; set; } = null!;

    public virtual ICollection<UserWriting> UserWritings { get; set; } = new List<UserWriting>();
}
