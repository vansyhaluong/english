using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class GroupStimulus
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public int DisplayOrder { get; set; }

    public byte Kind { get; set; }

    public string ContentOrUrl { get; set; } = null!;

    public virtual QuestionGroup Group { get; set; } = null!;
}
