using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class TranscriptCue
{
    public int Id { get; set; }

    public int ListeningLessonId { get; set; }

    public int Sequence { get; set; }

    public int StartMs { get; set; }

    public int EndMs { get; set; }

    public string Text { get; set; } = null!;

    public virtual ListeningLesson ListeningLesson { get; set; } = null!;
}
