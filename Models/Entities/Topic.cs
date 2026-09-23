using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class Topic
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    public virtual ICollection<LearningItem> LearningItems { get; set; } = new List<LearningItem>();
}
