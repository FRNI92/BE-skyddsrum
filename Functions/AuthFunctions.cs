using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Skyddsrum.Functions.Authentication;

namespace Skyddsrum.Functions.Functions;

public sealed class AuthFunctions(ICurrentUserReader currentUserReader)
{
    [Function("GetCurrentAdminUser")]
    public async Task<HttpResponseData> GetCurrentAdminUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/me")] HttpRequestData request)
    {
        var user = currentUserReader.Read(request);
        return await request.JsonAsync(user);
    }
}
