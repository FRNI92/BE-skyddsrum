using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Skyddsrum.Functions.Authentication;
using Skyddsrum.Functions.DTOs;
using Skyddsrum.Functions.Storage;

namespace Skyddsrum.Functions.Functions;

public sealed class UploadFunctions(
    IAdminAuthorization adminAuthorization,
    IBlobStorageService blobStorageService)
{
    [Function("InitImageUpload")]
    public async Task<HttpResponseData> InitImageUpload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/upload/init")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            adminAuthorization.Authorize(request);
        }
        catch
        {
            return request.CreateResponse(HttpStatusCode.Forbidden);
        }

        var payload = await JsonSerializer.DeserializeAsync<UploadInitRequestDto>(request.Body, JsonOptions.Default, cancellationToken);
        if (payload is null)
        {
            return await request.ErrorAsync("Invalid upload payload.", HttpStatusCode.BadRequest);
        }

        try
        {
            var result = await blobStorageService.CreateImageUploadAsync(payload, cancellationToken);
            return await request.JsonAsync(result);
        }
        catch (ArgumentException ex)
        {
            return await request.ErrorAsync(ex.Message, HttpStatusCode.BadRequest);
        }
    }
}
