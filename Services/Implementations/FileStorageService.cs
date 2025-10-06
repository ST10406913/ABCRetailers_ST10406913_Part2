using ABCRetailers.Models;
using ABCRetailers.Services.Interfaces;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ABCRetailers.Services.Implementations
{
    public class FileStorageService : IFileStorageService
    {
        private readonly ShareServiceClient _shareServiceClient;
        private readonly ILogger<FileStorageService> _logger;
        private readonly AzureStorageSettings _settings;

        public FileStorageService(IOptions<AzureStorageSettings> options, ILogger<FileStorageService> logger)
        {
            _settings = options.Value;
            _logger = logger;

            try
            {
                _shareServiceClient = new ShareServiceClient(_settings.ConnectionString);
                InitializeSharesAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize File Storage Service");
                throw;
            }
        }

        private async Task InitializeSharesAsync()
        {
            try
            {
                var shares = new[] { _settings.FileShareNames.Documents };

                foreach (var shareName in shares)
                {
                    var shareClient = _shareServiceClient.GetShareClient(shareName);
                    await shareClient.CreateIfNotExistsAsync();
                    _logger.LogInformation("File share {ShareName} initialized successfully", shareName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize file shares");
                throw;
            }
        }

        public async Task<bool> UploadFileAsync(IFormFile file, string shareName, string directoryPath = "")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is null or empty");
                }

                // Validate file size (20MB limit for file shares)
                if (file.Length > 20 * 1024 * 1024)
                {
                    throw new InvalidOperationException("File size exceeds 20MB limit");
                }

                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryPath);
                await directoryClient.CreateIfNotExistsAsync();

                var fileClient = directoryClient.GetFileClient(file.FileName);

                using var stream = file.OpenReadStream();
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream);

                _logger.LogInformation("File uploaded successfully: {FileName} to share: {ShareName}, directory: {DirectoryPath}",
                    file.FileName, shareName, directoryPath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to file share");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName, string shareName, string directoryPath = "")
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryPath);
                var fileClient = directoryClient.GetFileClient(fileName);

                var response = await fileClient.DeleteIfExistsAsync();
                _logger.LogInformation("File {FileName} deleted from share {ShareName}: {Success}",
                    fileName, shareName, response.Value);

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName} from share {ShareName}", fileName, shareName);
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string fileName, string shareName, string directoryPath = "")
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryPath);
                var fileClient = directoryClient.GetFileClient(fileName);

                var response = await fileClient.DownloadAsync();
                _logger.LogInformation("File {FileName} downloaded from share {ShareName}", fileName, shareName);

                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileName} from share {ShareName}", fileName, shareName);
                throw;
            }
        }

        public async Task<List<string>> GetFileListAsync(string shareName, string directoryPath = "")
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryPath);

                var files = new List<string>();
                await foreach (var fileItem in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!fileItem.IsDirectory)
                    {
                        files.Add(fileItem.Name);
                    }
                }

                _logger.LogInformation("Retrieved {Count} files from share {ShareName}", files.Count, shareName);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file list from share {ShareName}", shareName);
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string fileName, string shareName, string directoryPath = "")
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryPath);
                var fileClient = directoryClient.GetFileClient(fileName);

                return await fileClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file {FileName} exists in share {ShareName}", fileName, shareName);
                throw;
            }
        }
    }
}