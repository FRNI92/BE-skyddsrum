namespace Skyddsrum.Functions.Repositories;

public sealed class CosmosOptions
{
    public const string SectionName = "Cosmos";

    public string DatabaseName { get; init; } = "skyddsrum";
    public string ArticlesContainerName { get; init; } = "articles";
}
