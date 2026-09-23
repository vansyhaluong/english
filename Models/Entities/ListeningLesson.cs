using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class ListeningLesson
{
    public int LearningItemId { get; set; }

    public string AudioUrl { get; set; } = null!;

    public int? SubtitleFileId { get; set; }

    public int TranscriptRevision { get; set; }

    public virtual LearningItem LearningItem { get; set; } = null!;

    public virtual StoredFile? SubtitleFile { get; set; }

    public virtual ICollection<TranscriptCue> TranscriptCues { get; set; } = new List<TranscriptCue>();
}
