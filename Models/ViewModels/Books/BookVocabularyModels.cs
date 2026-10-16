using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace English.Models.ViewModels.Books;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class BookVocabularyEntry
{
    [Required, StringLength(160)] public string Word { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string MeaningVi { get; set; } = string.Empty;
    [StringLength(120)] public string? Ipa { get; set; }
    [StringLength(80)] public string? PartOfSpeech { get; set; }
}

public sealed class ChapterVocabularyInput : BookVersionInput, IValidatableObject
{
    public List<BookVocabularyEntry> Entries { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Entries is null || Entries.Count > BookVocabularyNotes.MaximumEntries)
        {
            yield return new("Use at most 200 vocabulary notes.", [nameof(Entries)]);
            yield break;
        }
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry is null)
            {
                yield return new("Invalid vocabulary note.", [$"Entries[{i}]"]);
                continue;
            }
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(entry, new(entry), results, true);
            foreach (var result in results)
                yield return new(result.ErrorMessage, result.MemberNames.Select(member => $"Entries[{i}].{member}"));
            if (!string.IsNullOrWhiteSpace(entry.Word) && !words.Add(BookVocabularyNotes.MatchKey(entry.Word)))
                yield return new("Each word or phrase must be unique within a chapter.", [$"Entries[{i}].Word"]);
        }
    }
}

public sealed record AdminChapterVocabularyPage(int BookId, int ChapterId, string BookTitle, string ChapterTitle,
    ChapterVocabularyInput Input, string? LegacyText);

// Stored in the existing nvarchar(max) field, independently of ContentEn.
public static class BookVocabularyNotes
{
    public const int MaximumEntries = 200;
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class Document
    {
        public int Version { get; set; }
        public List<BookVocabularyEntry> Entries { get; set; } = [];
        public string? LegacyText { get; set; }
    }

    public static string MatchKey(string word) => Regex.Replace(word.Trim(), @"\s+", " ");

    public static Document Read(string? source)
    {
        if (string.IsNullOrEmpty(source)) return new() { Version = 1 };
        try
        {
            var document = JsonSerializer.Deserialize<Document>(source, Options);
            if (document is { Version: 1, Entries: not null } && document.Entries.Count <= MaximumEntries)
            {
                var input = new ChapterVocabularyInput { Entries = document.Entries };
                if (!input.Validate(new(input)).Any()) return document;
            }
        }
        catch (JsonException) { }
        // Plain text, malformed JSON and unknown formats remain intact, never silently discarded.
        return new() { Version = 1, LegacyText = source };
    }

    public static string Write(List<BookVocabularyEntry> entries, string? existing)
    {
        var document = Read(existing);
        document.Entries = entries;
        return JsonSerializer.Serialize(document, Options);
    }
}
