using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Skyddsrum.Functions.Models;

namespace Skyddsrum.Functions.Repositories;

public interface IArticleRepository
{
    Task<IReadOnlyCollection<Article>> GetPublishedAsync(string? query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Article>> GetAllAsync(CancellationToken cancellationToken);
    Task<Article?> GetBySlugAsync(string slug, bool includeDrafts, CancellationToken cancellationToken);
    Task<Article?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Article> UpsertAsync(Article article, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}

public sealed class ArticleRepository : IArticleRepository
{
    private readonly Container _container;

    public ArticleRepository(CosmosClient cosmosClient, IOptions<CosmosOptions> options)
    {
        var value = options.Value;
        _container = cosmosClient.GetContainer(value.DatabaseName, value.ArticlesContainerName);
    }

    public Task<IReadOnlyCollection<Article>> GetPublishedAsync(string? query, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?> { ["@status"] = ArticleStatuses.Published };
        var sql = "SELECT * FROM c WHERE c.status = @status";

        if (!string.IsNullOrWhiteSpace(query))
        {
            sql += " AND (CONTAINS(LOWER(c.title), @query) OR CONTAINS(LOWER(c.description), @query) OR CONTAINS(LOWER(c.category), @query))";
            parameters["@query"] = query.Trim().ToLowerInvariant();
        }

        sql += " ORDER BY c.publishedAt DESC";
        return QueryAsync(sql, parameters, cancellationToken);
    }

    public Task<IReadOnlyCollection<Article>> GetAllAsync(CancellationToken cancellationToken) =>
        QueryAsync("SELECT * FROM c ORDER BY c.updatedAt DESC", new Dictionary<string, object?>(), cancellationToken);

    public async Task<Article?> GetBySlugAsync(string slug, bool includeDrafts, CancellationToken cancellationToken)
    {
        var sql = includeDrafts
            ? "SELECT * FROM c WHERE c.slug = @slug"
            : "SELECT * FROM c WHERE c.slug = @slug AND c.status = @status";

        var parameters = new Dictionary<string, object?>
        {
            ["@slug"] = slug,
            ["@status"] = ArticleStatuses.Published
        };

        return (await QueryAsync(sql, parameters, cancellationToken)).FirstOrDefault();
    }

    public async Task<Article?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<Article>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Article> UpsertAsync(Article article, CancellationToken cancellationToken)
    {
        var response = await _container.UpsertItemAsync(article, new PartitionKey(article.Id), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _container.DeleteItemAsync<Article>(id, new PartitionKey(id), cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyCollection<Article>> QueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(sql);
        foreach (var parameter in parameters)
        {
            query = query.WithParameter(parameter.Key, parameter.Value);
        }

        var results = new List<Article>();
        using var iterator = _container.GetItemQueryIterator<Article>(query);

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }

        return results;
    }
}
