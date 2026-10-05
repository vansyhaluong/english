using System.ComponentModel.DataAnnotations;
using English.Interfaces;

namespace English.Models.ViewModels.AdminClassifications;

public sealed class ClassificationInputModel
{
    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.RequiredClassificationName))]
    [StringLength(200, ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = nameof(SharedResource.MaxClassificationName))]
    [Display(Name = nameof(SharedResource.ClassificationName), ResourceType = typeof(SharedResource))]
    public string Name { get; set; } = string.Empty;

    [Display(Name = nameof(SharedResource.ClassificationDescription), ResourceType = typeof(SharedResource))]
    public string? Description { get; set; }

    public string? RowVersion { get; set; }
}

public sealed class ClassificationDeleteInputModel
{
    public string? RowVersion { get; set; }
}

public sealed record ClassificationIndexViewModel(ClassificationKind Kind, IReadOnlyList<ClassificationSummary> Items);
public sealed record ClassificationFormViewModel(ClassificationKind Kind, int? Id, ClassificationInputModel Input);
public sealed record ClassificationDeleteViewModel(ClassificationKind Kind, ClassificationDetails Item, ClassificationDeleteInputModel Input);
public sealed record LevelsViewModel(IReadOnlyList<LevelSummary> Levels);
