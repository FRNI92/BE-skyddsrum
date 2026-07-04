namespace Skyddsrum.Functions.Models;

public sealed class Article
{
    [Newtonsoft.Json.JsonProperty("id")]
    public required string Id { get; init; }

    [Newtonsoft.Json.JsonProperty("slug")]
    public required string Slug { get; set; }

    [Newtonsoft.Json.JsonProperty("title")]
    public required string Title { get; set; }

    [Newtonsoft.Json.JsonProperty("description")]
    public required string Description { get; set; }

    [Newtonsoft.Json.JsonProperty("category")]
    public required string Category { get; set; }

    [Newtonsoft.Json.JsonProperty("content")]
    public required string Content { get; set; }

    [Newtonsoft.Json.JsonProperty("status")]
    public string Status { get; set; } = ArticleStatuses.Draft;

    [Newtonsoft.Json.JsonProperty("authorName")]
    public string AuthorName { get; set; } = "Skyddsrumsgruppen";

    [Newtonsoft.Json.JsonProperty("imageUrl")]
    public string? ImageUrl { get; set; }

    [Newtonsoft.Json.JsonProperty("imageAlt")]
    public string? ImageAlt { get; set; }

    [Newtonsoft.Json.JsonProperty("publishedAt")]
    public DateTimeOffset? PublishedAt { get; set; }

    [Newtonsoft.Json.JsonProperty("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
