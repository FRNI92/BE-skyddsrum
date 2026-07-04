using Microsoft.Azure.Functions.Worker.Http;
using Skyddsrum.Functions.Models;

namespace Skyddsrum.Functions.Authentication;

public interface IAdminAuthorization
{
    CurrentUser Authorize(HttpRequestData request);
}

public sealed class AdminAuthorization(ICurrentUserReader currentUserReader) : IAdminAuthorization
{
    public CurrentUser Authorize(HttpRequestData request)
    {
        var user = currentUserReader.Read(request);
        if (!user.IsAuthenticated || !user.IsAdmin)
        {
            throw new UnauthorizedAccessException("Admin role is required.");
        }

        return user;
    }
}
