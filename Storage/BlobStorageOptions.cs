namespace Skyddsrum.Functions.Storage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string? ConnectionString { get; init; }
    public string ImagesContainerName { get; init; } = "images";
}
