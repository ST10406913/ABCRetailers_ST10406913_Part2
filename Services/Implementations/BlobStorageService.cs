using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.Models;

namespace ABCRetailers.Services.Implementations
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;
        private readonly AzureStorageSettings _settings;

        public BlobStorageService(IOptions<AzureStorageSettings> options, ILogger<BlobStorageService> logger)
        {
            _settings = options.Value;
            _logger = logger;

            try
            {
                _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
                InitializeContainersAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Blob Storage Service");
                throw;
            }
        }

        private async Task InitializeContainersAsync()
        {
            try
            {
                var containers = new[] { _settings.BlobContainerNames.ProductImages, _settings.BlobContainerNames.Documents };

                foreach (var containerName in containers)
                {
                    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                    await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
                    _logger.LogInformation("Blob container {ContainerName} initialized successfully", containerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize blob containers");
                throw;
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is null or empty");
                }

                // Validate file size (10MB limit)
                if (file.Length > 10 * 1024 * 1024)
                {
                    throw new InvalidOperationException("File size exceeds 10MB limit");
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new InvalidOperationException($"File type {fileExtension} is not allowed");
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                // Create unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var blobClient = containerClient.GetBlobClient(fileName);

                // Upload file
                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });

                _logger.LogInformation("File uploaded successfully: {FileName} to container: {ContainerName}", fileName, containerName);
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to blob storage");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName, string containerName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    throw new ArgumentException("File name cannot be null or empty");
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                var response = await blobClient.DeleteIfExistsAsync();
                _logger.LogInformation("File {FileName} deleted from container {ContainerName}: {Success}",
                    fileName, containerName, response.Value);

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName} from container {ContainerName}", fileName, containerName);
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string fileName, string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                var response = await blobClient.DownloadAsync();
                _logger.LogInformation("File {FileName} downloaded from container {ContainerName}", fileName, containerName);

                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileName} from container {ContainerName}", fileName, containerName);
                throw;
            }
        }

        public async Task<List<string>> GetBlobListAsync(string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobs = new List<string>();

                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    blobs.Add(blobItem.Name);
                }

                _logger.LogInformation("Retrieved {Count} blobs from container {ContainerName}", blobs.Count, containerName);
                return blobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blob list from container {ContainerName}", containerName);
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string fileName, string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                return await blobClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file {FileName} exists in container {ContainerName}", fileName, containerName);
                throw;
            }
        }
    }
}