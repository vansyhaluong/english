namespace English.Models;

public sealed record VocabularyWord(string Word, string Meaning, string Pronunciation, string PartOfSpeech, string Example);
public sealed record VocabularyDeck(string Id, string Icon, string Title, string Level, string Description, IReadOnlyList<VocabularyWord> Words);
public sealed record VocabularyViewModel(IReadOnlyList<VocabularyDeck> Decks, string? Level, string? Search, int Page, int TotalPages, int TotalCount);
