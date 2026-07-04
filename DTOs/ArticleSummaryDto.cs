using Skyddsrum.Functions.Models;

namespace Skyddsrum.Functions.DTOs;

public sealed class ArticleSummaryDto
{
    public required string Id { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageAlt { get; init; }

    public static ArticleSummaryDto FromArticle(Article article) => new()
    {
        Id = article.Id,
        Slug = article.Slug,
        Title = article.Title,
        Description = article.Description,
        Category = article.Category,
        Status = article.Status,
        PublishedAt = article.PublishedAt,
        UpdatedAt = article.UpdatedAt,
        ImageUrl = article.ImageUrl,
        ImageAlt = article.ImageAlt
    };
}
