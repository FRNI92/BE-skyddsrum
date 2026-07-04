using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Skyddsrum.Functions.Authentication;
using Skyddsrum.Functions.DTOs;
using Skyddsrum.Functions.Models;
using Skyddsrum.Functions.Services;

namespace Skyddsrum.Functions.Functions;

public sealed class AdminArticlesFunctions(
    IAdminAuthorization adminAuthorization,
    IArticleService articleService)
{
    [Function("GetAdminArticles")]
    public async Task<HttpResponseData> GetAdminArticles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/articles")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(request, out var unauthorized))
        {
            return unauthorized!;
        }

        var articles = await articleService.GetAdminArticlesAsync(cancellationToken);
        return await request.JsonAsync(articles);
    }

    [Function("GetAdminArticle")]
    public async Task<HttpResponseData> GetAdminArticle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/articles/{id}")] HttpRequestData request,
        string id,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(request, out var unauthorized))
        {
            return unauthorized!;
        }

        var article = await articleService.GetByIdAsync(id, cancellationToken);
        return article is null
            ? await request.ErrorAsync("Article not found.", HttpStatusCode.NotFound)
            : await request.JsonAsync(article);
    }

    [Function("CreateArticle")]
    public async Task<HttpResponseData> CreateArticle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/articles")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(request, out var unauthorized, out var user))
        {
            return unauthorized!;
        }

        var payload = await ReadArticleInputAsync(request, cancellationToken);
        if (payload is null)
        {
            return await request.ErrorAsync("Invalid article payload.", HttpStatusCode.BadRequest);
        }

        try
        {
            var article = await articleService.CreateAsync(payload, user.Name ?? "Skyddsrumsgruppen", cancellationToken);
            return await request.JsonAsync(article, HttpStatusCode.Created);
        }
        catch (ArgumentException ex)
        {
            return await request.ErrorAsync(ex.Message, HttpStatusCode.BadRequest);
        }
    }

    [Function("UpdateArticle")]
    public async Task<HttpResponseData> UpdateArticle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "admin/articles/{id}")] HttpRequestData request,
        string id,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(request, out var unauthorized))
        {
            return unauthorized!;
        }

        var payload = await ReadArticleInputAsync(request, cancellationToken);
        if (payload is null)
        {
            return await request.ErrorAsync("Invalid article payload.", HttpStatusCode.BadRequest);
        }

        try
        {
            var article = await articleService.UpdateAsync(id, payload, cancellationToken);
            return article is null
                ? await request.ErrorAsync("Article not found.", HttpStatusCode.NotFound)
                : await request.JsonAsync(article);
        }
        catch (ArgumentException ex)
        {
            return await request.ErrorAsync(ex.Message, HttpStatusCode.BadRequest);
        }
    }

    [Function("DeleteArticle")]
    public async Task<HttpResponseData> DeleteArticle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "admin/articles/{id}")] HttpRequestData request,
        string id,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(request, out var unauthorized))
        {
            return unauthorized!;
        }

        await articleService.DeleteAsync(id, cancellationToken);
        return request.NoContent();
    }

    [Function("PublishArticle")]
    public async Task<HttpResponseData> PublishArticle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/articles/{id}/publish")] HttpRequestData request,
        string id,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(request, out var unauthorized))
        {
            return unauthorized!;
        }

        var article = await articleService.PublishAsync(id, cancellationToken);
        return article is null
            ? await request.ErrorAsync("Article not found.", HttpStatusCode.NotFound)
            : await request.JsonAsync(article);
    }

    private bool TryAuthorize(HttpRequestData request, out HttpResponseData? unauthorized) =>
        TryAuthorize(request, out unauthorized, out _);

    private bool TryAuthorize(HttpRequestData request, out HttpResponseData? unauthorized, out CurrentUser user)
    {
        try
        {
            user = adminAuthorization.Authorize(request);
            unauthorized = null;
            return true;
        }
        catch
        {
            user = new CurrentUser { IsAuthenticated = false };
            unauthorized = request.CreateResponse(HttpStatusCode.Forbidden);
            return false;
        }
    }

    private static Task<ArticleInputDto?> ReadArticleInputAsync(HttpRequestData request, CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsync<ArticleInputDto>(request.Body, JsonOptions.Default, cancellationToken).AsTask();
}
