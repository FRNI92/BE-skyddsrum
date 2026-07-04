namespace Skyddsrum.Functions.DTOs;

public sealed class UploadInitRequestDto
{
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
}

public sealed class UploadInitResponseDto
{
    public required string UploadUrl { get; init; }
    public required string BlobUrl { get; init; }
    public required string ImageId { get; init; }
}
