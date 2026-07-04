using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Skyddsrum.Functions.DTOs;

namespace Skyddsrum.Functions.Storage;

public interface IBlobStorageService
{
    Task<UploadInitResponseDto> CreateImageUploadAsync(UploadInitRequestDto request, CancellationToken cancellationToken);
}

public sealed class BlobStorageService(IOptions<BlobStorageOptions> options) : IBlobStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/avif"
    };

    public async Task<UploadInitResponseDto> CreateImageUploadAsync(UploadInitRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new ArgumentException("FileName and contentType are required.");
        }

        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            throw new ArgumentException("Unsupported image type.");
        }

        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.ConnectionString))
        {
            throw new InvalidOperationException("Missing BlobStorage:ConnectionString setting.");
        }

        var container = new BlobContainerClient(value.ConnectionString, value.ImagesContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var imageId = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(request.FileName);
        var blobName = $"{DateTimeOffset.UtcNow:yyyy/MM}/{imageId}{extension}";
        var blob = container.GetBlobClient(blobName);

        var sas = blob.GenerateSasUri(BlobSasPermissions.Create | BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddMinutes(15));

        return new UploadInitResponseDto
        {
            UploadUrl = sas.ToString(),
            BlobUrl = blob.Uri.ToString(),
            ImageId = imageId
        };
    }
}
