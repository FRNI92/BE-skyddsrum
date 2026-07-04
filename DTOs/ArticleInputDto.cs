namespace Skyddsrum.Functions.DTOs;

public sealed class ArticleInputDto
{
    public string? Slug { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? Content { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageAlt { get; init; }
}
