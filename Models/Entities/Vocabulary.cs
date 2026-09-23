using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class Vocabulary
{
    public int LearningItemId { get; set; }

    public string Word { get; set; } = null!;

    public string Pronunciation { get; set; } = null!;

    public string PartOfSpeech { get; set; } = null!;

    public string Example { get; set; } = null!;

    public string? AudioUrl { get; set; }

    public string? ImageUrl { get; set; }

    public virtual LearningItem LearningItem { get; set; } = null!;

    public virtual ICollection<VocabularyMeaning> VocabularyMeanings { get; set; } = new List<VocabularyMeaning>();
}
