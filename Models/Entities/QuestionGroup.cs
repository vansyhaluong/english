using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class QuestionGroup
{
    public int Id { get; set; }

    public int PartId { get; set; }

    public int DisplayOrder { get; set; }

    public byte StimulusKind { get; set; }

    public virtual ICollection<GroupStimulus> GroupStimuli { get; set; } = new List<GroupStimulus>();

    public virtual ToeicPart Part { get; set; } = null!;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
