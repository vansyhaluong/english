using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class StoredFile
{
    public int Id { get; set; }

    public string StorageKey { get; set; } = null!;

    public byte Kind { get; set; }

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public string OriginalName { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid UploadedByUserId { get; set; }

    public virtual ICollection<AspNetUser> AspNetUsers { get; set; } = new List<AspNetUser>();

    public virtual ICollection<ListeningLesson> ListeningLessons { get; set; } = new List<ListeningLesson>();

    public virtual AspNetUser UploadedByUser { get; set; } = null!;
}
