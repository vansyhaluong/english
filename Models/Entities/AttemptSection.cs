using System;
using System.Collections.Generic;

namespace English.Models.Entities;

public partial class AttemptSection
{
    public Guid Id { get; set; }

    public Guid AttemptId { get; set; }

    public byte Kind { get; set; }

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset EndsAtUtc { get; set; }

    public DateTimeOffset? LockedAtUtc { get; set; }

    public virtual Attempt Attempt { get; set; } = null!;
}
