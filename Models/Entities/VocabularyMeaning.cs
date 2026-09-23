using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class VocabularyMeaning
{
    public int Id { get; set; }

    public int VocabularyId { get; set; }

    public string MeaningVi { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Vocabulary Vocabulary { get; set; } = null!;
}
