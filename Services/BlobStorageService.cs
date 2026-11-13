using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Assignment1EventHandler.Services
{
    public class BlobStorageService
    {
        private const long MaxFileSize = 5 * 1024 * 1024;

        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(
            BlobServiceClient blobServiceClient,
            IConfiguration configuration)
        {
            var containerName =
                configuration["AzureBlobStorage:ContainerName"];

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new InvalidOperationException(
                    "Blob container name is missing.");
            }

            _containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
                throw new InvalidOperationException(
                    "Please select an image.");

            if (image.Length > MaxFileSize)
                throw new InvalidOperationException(
                    "Image must be 5 MB or smaller.");

            var extension =
                Path.GetExtension(image.FileName).ToLowerInvariant();

            var allowedTypes = new Dictionary<string, string>
            {
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png",
                [".gif"] = "image/gif",
                [".webp"] = "image/webp"
            };

            if (!allowedTypes.TryGetValue(extension, out var contentType)
                || !string.Equals(
                    image.ContentType,
                    contentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG, GIF, and WEBP images are allowed.");
            }

            // Generate a unique file name.
            string blobName = $"{Guid.NewGuid():N}{extension}";

            BlobClient blobClient =
                _containerClient.GetBlobClient(blobName);

            await using var stream = image.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                });

            // Return the public URL.
            return blobClient.Uri.ToString();
        }
    }
}
