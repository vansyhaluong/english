using System.ComponentModel.DataAnnotations;

namespace English.Models.ViewModels.Books;

public sealed class AdminBookUploadInput
{
    [Required, StringLength(300)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Author { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int TopicId { get; set; }
    public string? Description { get; set; }
    public IFormFile? CoverImage { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? RowVersion { get; set; }

    public BookInput ToBookInput() => new()
    {
        Title = Title, Author = Author, TopicId = TopicId, Description = Description,
        IsVisible = IsVisible, RowVersion = RowVersion
    };
}
