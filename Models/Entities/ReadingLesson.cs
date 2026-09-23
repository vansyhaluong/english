using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class ReadingLesson
{
    public int LearningItemId { get; set; }

    public string ContentEn { get; set; } = null!;

    public string? TranslationVi { get; set; }

    public string? VocabularyNotes { get; set; }

    public virtual LearningItem LearningItem { get; set; } = null!;
}
