using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class Level
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public byte SortOrder { get; set; }

    public virtual ICollection<AspNetUser> AspNetUsers { get; set; } = new List<AspNetUser>();

    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    public virtual ICollection<LearningItem> LearningItems { get; set; } = new List<LearningItem>();
}
