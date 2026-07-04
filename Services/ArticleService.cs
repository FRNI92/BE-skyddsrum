using Skyddsrum.Functions.DTOs;
using Skyddsrum.Functions.Models;
using Skyddsrum.Functions.Repositories;

namespace Skyddsrum.Functions.Services;

public interface IArticleService
{
    Task<IReadOnlyCollection<ArticleSummaryDto>> GetPublishedAsync(string? query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ArticleSummaryDto>> GetAdminArticlesAsync(CancellationToken cancellationToken);
    Task<ArticleDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<ArticleDto?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<ArticleDto> CreateAsync(ArticleInputDto input, string authorName, CancellationToken cancellationToken);
    Task<ArticleDto?> UpdateAsync(string id, ArticleInputDto input, CancellationToken cancellationToken);
    Task<ArticleDto?> PublishAsync(string id, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}

public sealed class ArticleService(IArticleRepository repository) : IArticleService
{
    public async Task<IReadOnlyCollection<ArticleSummaryDto>> GetPublishedAsync(string? query, CancellationToken cancellationToken)
    {
        var articles = await repository.GetPublishedAsync(query, cancellationToken);
        return articles.Select(ArticleSummaryDto.FromArticle).ToArray();
    }

    public async Task<IReadOnlyCollection<ArticleSummaryDto>> GetAdminArticlesAsync(CancellationToken cancellationToken)
    {
        var articles = await repository.GetAllAsync(cancellationToken);
        return articles.Select(ArticleSummaryDto.FromArticle).ToArray();
    }

    public async Task<ArticleDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var article = await repository.GetBySlugAsync(slug, includeDrafts: false, cancellationToken);
        return article is null ? null : ArticleDto.FromArticle(article);
    }

    public async Task<ArticleDto?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var article = await repository.GetByIdAsync(id, cancellationToken);
        return article is null ? null : ArticleDto.FromArticle(article);
    }

    public async Task<ArticleDto> CreateAsync(ArticleInputDto input, string authorName, CancellationToken cancellationToken)
    {
        Validate(input);

        var article = new Article
        {
            Id = Guid.NewGuid().ToString("N"),
            Slug = NormalizeSlug(input.Slug!),
            Title = input.Title!.Trim(),
            Description = input.Description!.Trim(),
            Category = input.Category!.Trim(),
            Content = input.Content!.Trim(),
            AuthorName = string.IsNullOrWhiteSpace(authorName) ? "Skyddsrumsgruppen" : authorName,
            ImageUrl = NormalizeOptional(input.ImageUrl),
            ImageAlt = NormalizeOptional(input.ImageAlt),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return ArticleDto.FromArticle(await repository.UpsertAsync(article, cancellationToken));
    }

    public async Task<ArticleDto?> UpdateAsync(string id, ArticleInputDto input, CancellationToken cancellationToken)
    {
        Validate(input);

        var article = await repository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return null;
        }

        article.Slug = NormalizeSlug(input.Slug!);
        article.Title = input.Title!.Trim();
        article.Description = input.Description!.Trim();
        article.Category = input.Category!.Trim();
        article.Content = input.Content!.Trim();
        article.ImageUrl = NormalizeOptional(input.ImageUrl);
        article.ImageAlt = NormalizeOptional(input.ImageAlt);
        article.UpdatedAt = DateTimeOffset.UtcNow;

        return ArticleDto.FromArticle(await repository.UpsertAsync(article, cancellationToken));
    }

    public async Task<ArticleDto?> PublishAsync(string id, CancellationToken cancellationToken)
    {
        var article = await repository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return null;
        }

        article.Status = ArticleStatuses.Published;
        article.PublishedAt ??= DateTimeOffset.UtcNow;
        article.UpdatedAt = DateTimeOffset.UtcNow;

        return ArticleDto.FromArticle(await repository.UpsertAsync(article, cancellationToken));
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken) => repository.DeleteAsync(id, cancellationToken);

    private static void Validate(ArticleInputDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Slug) ||
            string.IsNullOrWhiteSpace(input.Title) ||
            string.IsNullOrWhiteSpace(input.Description) ||
            string.IsNullOrWhiteSpace(input.Category) ||
            string.IsNullOrWhiteSpace(input.Content))
        {
            throw new ArgumentException("Slug, title, description, category and content are required.");
        }
    }

    private static string NormalizeSlug(string value) =>
        value.Trim().ToLowerInvariant().Replace(' ', '-');

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
