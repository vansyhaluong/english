using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class ToeicSection
{
    public int Id { get; set; }

    public int TestId { get; set; }

    public byte Kind { get; set; }

    public string? FullAudioUrl { get; set; }

    public virtual ToeicTest Test { get; set; } = null!;

    public virtual ICollection<ToeicPart> ToeicParts { get; set; } = new List<ToeicPart>();
}
