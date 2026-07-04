using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Skyddsrum.Functions.Models;

namespace Skyddsrum.Functions.Authentication;

public interface ICurrentUserReader
{
    CurrentUser Read(HttpRequestData request);
}

public sealed class CurrentUserReader : ICurrentUserReader
{
    public CurrentUser Read(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues("x-ms-client-principal", out var values))
        {
            return new CurrentUser { IsAuthenticated = false };
        }

        var encoded = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return new CurrentUser { IsAuthenticated = false };
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var principal = JsonSerializer.Deserialize<ClientPrincipal>(json, JsonOptions.Default);

            return new CurrentUser
            {
                IsAuthenticated = principal is not null,
                Name = principal?.UserDetails,
                Roles = principal?.UserRoles ?? Array.Empty<string>()
            };
        }
        catch
        {
            return new CurrentUser { IsAuthenticated = false };
        }
    }

    private sealed class ClientPrincipal
    {
        public string? UserDetails { get; init; }
        public string[] UserRoles { get; init; } = Array.Empty<string>();
    }
}
