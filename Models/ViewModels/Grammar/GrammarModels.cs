using System.ComponentModel.DataAnnotations;

namespace English.Models.ViewModels.Grammar;

public sealed class GrammarInputModel
{
    private string title = string.Empty;
    [Required(ErrorMessage = "GrammarRequired"), StringLength(300, ErrorMessage = "GrammarTooLong")]
    public string Title { get => title; set => title = value?.Trim() ?? string.Empty; }
    public string? Description { get; set; }
    [Required(ErrorMessage = "GrammarRequired")]
    public string Formula { get; set; } = string.Empty;
    [Required(ErrorMessage = "GrammarRequired")]
    public string Usage { get; set; } = string.Empty;
    [Required(ErrorMessage = "GrammarRequired")]
    public string Examples { get; set; } = string.Empty;
    public string? Notes { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "GrammarClassificationRequired")]
    public int LevelId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "GrammarClassificationRequired")]
    public int GrammarGroupId { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? RowVersion { get; set; }
}

public sealed class GrammarQuery
{
    [StringLength(300, ErrorMessage = "GrammarTooLong")]
    public string? Search { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "GrammarClassificationRequired")]
    public int? LevelId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "GrammarClassificationRequired")]
    public int? GrammarGroupId { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class GrammarDeleteInputModel
{
    public string? RowVersion { get; set; }
}

public sealed class GrammarProgressInputModel
{
    [Required(ErrorMessage = "GrammarRequired")]
    public bool? IsLearned { get; set; }
}

public sealed record GrammarOption(int Id, string Name);
public sealed record GrammarChoices(IReadOnlyList<GrammarOption> Levels, IReadOnlyList<GrammarOption> Groups);
public sealed class GrammarDetailsViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int LevelId { get; init; }
    public string Level { get; init; } = string.Empty;
    public int GrammarGroupId { get; init; }
    public string GrammarGroup { get; init; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Examples { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsVisible { get; init; }
    public bool IsDeleted { get; init; }
    public bool IsLearned { get; init; }
    public byte[] RowVersion { get; init; } = [];
}
public sealed record GrammarListViewModel(GrammarQuery Query, GrammarChoices Choices,
    IReadOnlyList<GrammarDetailsViewModel> Items, int TotalPages, bool IsAdmin);
public sealed record GrammarWorkspaceViewModel(GrammarQuery Query, GrammarChoices Choices,
    IReadOnlyList<GrammarDetailsViewModel> Items, GrammarDetailsViewModel? Lesson);
public sealed record GrammarFormViewModel(int? Id, GrammarInputModel Input, GrammarChoices Choices);
public sealed record GrammarDeleteViewModel(GrammarDetailsViewModel Item, GrammarDeleteInputModel Input);
