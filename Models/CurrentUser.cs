namespace Skyddsrum.Functions.Models;

public sealed class CurrentUser
{
    public bool IsAuthenticated { get; init; }
    public string? Name { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    public bool IsAdmin => Roles.Contains("admin", StringComparer.OrdinalIgnoreCase);
}
