using System.ComponentModel.DataAnnotations;

namespace English.Models.ViewModels.Vocabulary;

public static class VocabularyPartsOfSpeech
{
    public static IReadOnlyList<string> Values { get; } = Array.AsReadOnly(new[]
    {
        "Noun", "Verb", "Adjective", "Adverb", "Pronoun", "Preposition",
        "Conjunction", "Interjection", "Phrase", "Other"
    });
}

public sealed class VocabularyInputModel
{
    private string title = string.Empty;
    private string word = string.Empty;
    private string pronunciation = string.Empty;
    private string partOfSpeech = string.Empty;
    private string example = string.Empty;
    // Normalize during binding, before MVC checks required fields and length limits.
    [Required(ErrorMessage = "VocabRequired"), StringLength(300, ErrorMessage = "VocabTooLong")]
    public string Title { get => title; set => title = value?.Trim() ?? string.Empty; }
    [Required(ErrorMessage = "VocabRequired"), StringLength(200, ErrorMessage = "VocabTooLong")]
    public string Word { get => word; set => word = value?.Trim() ?? string.Empty; }
    [Required(ErrorMessage = "VocabRequired"), StringLength(300, ErrorMessage = "VocabTooLong")]
    public string Pronunciation { get => pronunciation; set => pronunciation = value?.Trim() ?? string.Empty; }
    [Required(ErrorMessage = "VocabRequired"), StringLength(50, ErrorMessage = "VocabTooLong")]
    public string PartOfSpeech { get => partOfSpeech; set => partOfSpeech = value?.Trim() ?? string.Empty; }
    [Required(ErrorMessage = "VocabRequired")]
    public string Example { get => example; set => example = value?.Trim() ?? string.Empty; }
    public string? Description { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "VocabClassificationRequired")]
    public int LevelId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "VocabClassificationRequired")]
    public int TopicId { get; set; }
    [StringLength(2048, ErrorMessage = "VocabTooLong")]
    public string? ImageUrl { get; set; }
    [StringLength(2048, ErrorMessage = "VocabTooLong")]
    public string? AudioUrl { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? RowVersion { get; set; }
    public List<VocabularyMeaningInputModel> Meanings { get; set; } = [];
}

public sealed class VocabularyMeaningInputModel
{
    private string meaningVi = string.Empty;
    [Range(0, int.MaxValue, ErrorMessage = "VocabInvalidMeaning")]
    public int Id { get; set; }
    [Required(ErrorMessage = "VocabMeaningRequired"), StringLength(1000, ErrorMessage = "VocabTooLong")]
    public string MeaningVi { get => meaningVi; set => meaningVi = value?.Trim() ?? string.Empty; }
}

public sealed class VocabularyQuery
{
    [StringLength(300, ErrorMessage = "VocabTooLong")]
    public string? Search { get; set; }
    public int? LevelId { get; set; }
    public int? TopicId { get; set; }
    public bool? Learned { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class VocabularyDeleteInputModel
{
    public string? RowVersion { get; set; }
}

public sealed class VocabularyProgressInputModel
{
    [Required(ErrorMessage = "VocabRequired")]
    public bool? IsLearned { get; set; }
}

public sealed record VocabularyOption(int Id, string Name);
public sealed record VocabularyChoices(IReadOnlyList<VocabularyOption> Levels, IReadOnlyList<VocabularyOption> Topics);
public sealed record VocabularyMeaningViewModel(int Id, string MeaningVi, int DisplayOrder);
public sealed class VocabularyDetailsViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Word { get; init; } = string.Empty;
    public string Pronunciation { get; init; } = string.Empty;
    public string PartOfSpeech { get; init; } = string.Empty;
    public string Example { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int LevelId { get; init; }
    public string Level { get; init; } = string.Empty;
    public int TopicId { get; init; }
    public string Topic { get; init; } = string.Empty;
    public bool IsVisible { get; init; }
    public bool IsDeleted { get; init; }
    public bool IsLearned { get; init; }
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public byte[] RowVersion { get; init; } = [];
    public List<VocabularyMeaningViewModel> Meanings { get; init; } = [];
}

public sealed record VocabularyListViewModel(VocabularyQuery Query, VocabularyChoices Choices,
    IReadOnlyList<VocabularyDetailsViewModel> Items, int TotalPages, bool IsAdmin);
public sealed record VocabularyFormViewModel(int? Id, VocabularyInputModel Input, VocabularyChoices Choices);
public sealed record VocabularyDeleteViewModel(VocabularyDetailsViewModel Item, VocabularyDeleteInputModel Input);
