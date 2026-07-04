using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace Skyddsrum.Functions.Functions;

public static class HttpResponseExtensions
{
    public static async Task<HttpResponseData> JsonAsync<T>(
        this HttpRequestData request,
        T value,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(value, JsonOptions.Default));
        return response;
    }

    public static async Task<HttpResponseData> ErrorAsync(
        this HttpRequestData request,
        string message,
        HttpStatusCode statusCode)
    {
        return await request.JsonAsync(new { error = message }, statusCode);
    }

    public static HttpResponseData NoContent(this HttpRequestData request) =>
        request.CreateResponse(HttpStatusCode.NoContent);
}
