using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class ToeicPart
{
    public int Id { get; set; }

    public int SectionId { get; set; }

    public byte Number { get; set; }

    public string? Instructions { get; set; }

    public int DisplayOrder { get; set; }

    public virtual ICollection<QuestionGroup> QuestionGroups { get; set; } = new List<QuestionGroup>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ToeicSection Section { get; set; } = null!;
}
