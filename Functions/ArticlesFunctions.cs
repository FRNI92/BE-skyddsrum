using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Skyddsrum.Functions.Services;

namespace Skyddsrum.Functions.Functions;

public sealed class ArticlesFunctions(IArticleService articleService)
{
    [Function("GetArticles")]
    public async Task<HttpResponseData> GetArticles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var queryValues = QueryHelpers.ParseQuery(request.Url.Query);
        var query = queryValues.TryGetValue("query", out var value) ? value.ToString() : null;
        var articles = await articleService.GetPublishedAsync(query, cancellationToken);
        return await request.JsonAsync(articles);
    }

    [Function("GetArticleBySlug")]
    public async Task<HttpResponseData> GetArticleBySlug(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{slug}")] HttpRequestData request,
        string slug,
        CancellationToken cancellationToken)
    {
        var article = await articleService.GetBySlugAsync(slug, cancellationToken);
        return article is null
            ? await request.ErrorAsync("Article not found.", HttpStatusCode.NotFound)
            : await request.JsonAsync(article);
    }
}
