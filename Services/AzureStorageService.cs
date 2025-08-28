using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace ABCRetailers.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly string _connectionString;
        private TableServiceClient _tableServiceClient;
        private BlobServiceClient _blobServiceClient;
        private QueueServiceClient _queueServiceClient;
        private ShareServiceClient _fileShareServiceClient;

        public AzureStorageService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task InitializeStorageAsync()
        {
            _tableServiceClient = new TableServiceClient(_connectionString);
            _blobServiceClient = new BlobServiceClient(_connectionString);
            _queueServiceClient = new QueueServiceClient(_connectionString);
            _fileShareServiceClient = new ShareServiceClient(_connectionString);
            await Task.CompletedTask;
        }

        // ---------------- TABLE STORAGE ----------------

        public async Task<List<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new()
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            var entities = new List<T>();

            await foreach (var entity in tableClient.QueryAsync<T>())
            {
                entities.Add(entity);
            }

            return entities;
        }

        public async Task<T> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey)
            where T : class, ITableEntity, new()
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
            return response.Value;
        }

        public async Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.AddEntityAsync(entity);
        }

        public async Task UpdateEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // ---------------- BLOB STORAGE ----------------

        public async Task<string> UploadImageAsync(string containerName, string blobName, Stream imageStream, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(blobName);

            var headers = new BlobHttpHeaders { ContentType = contentType };
            await blobClient.UploadAsync(imageStream, new BlobUploadOptions { HttpHeaders = headers });

            return blobClient.Uri.ToString();
        }

        public async Task<string> UploadFileAsync(string containerName, string blobName, Stream fileStream, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(blobName);

            var headers = new BlobHttpHeaders { ContentType = contentType };
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = headers });

            return blobClient.Uri.ToString();
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        // ---------------- QUEUE STORAGE ----------------

        public async Task SendMessageAsync(string queueName, string messageText)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(messageText);
        }

        public async Task<string> ReceiveMessageAsync(string queueName)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();

            QueueMessage[] retrievedMessage = await queueClient.ReceiveMessagesAsync(1);
            if (retrievedMessage.Length > 0)
            {
                var message = retrievedMessage[0];
                await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                return message.MessageText;
            }

            return null;
        }

        // ---------------- FILE SHARE STORAGE ----------------

        public async Task UploadToFileShareAsync(string shareName, string directoryName, string fileName, Stream fileStream)
        {
            var shareClient = _fileShareServiceClient.GetShareClient(shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            var fileClient = directoryClient.GetFileClient(fileName);
            await fileClient.CreateAsync(fileStream.Length);
            await fileClient.UploadRangeAsync(new HttpRange(0, fileStream.Length), fileStream);
        }

        public async Task<Stream> DownloadFromFileShareAsync(string shareName, string directoryName, string fileName)
        {
            var shareClient = _fileShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            var fileClient = directoryClient.GetFileClient(fileName);

            ShareFileDownloadInfo download = await fileClient.DownloadAsync();
            return download.Content;
        }

        // ---------------- HELPER ----------------

        private string GetTableName<T>()
        {
            return typeof(T).Name.ToLowerInvariant();
        }

        Task<List<T>> IAzureStorageService.GetAllEntitiesAsync<T>()
        {
            throw new NotImplementedException();
        }

        Task<T?> IAzureStorageService.GetEntityAsync<T>(string partitionkey, string rowkey) where T : class
        {
            throw new NotImplementedException();
        }

        Task<T> IAzureStorageService.AddEntityAsync<T>(T entity)
        {
            throw new NotImplementedException();
        }

        Task<T> IAzureStorageService.UpdateEntityAsync<T>(T entity)
        {
            throw new NotImplementedException();
        }

        Task IAzureStorageService.DeleteEntityAsync<T>(string partitionkey, string rowkey)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadImageAsync(IFormFile file, string containerName)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadToFileShareAsync(IFormFile file, string sharellane, string directoryName = "")
        {
            throw new NotImplementedException();
        }

        Task<byte[]> IAzureStorageService.DownloadFromFileShareAsync(string sharelane, string filellane, string directoryName)
        {
            throw new NotImplementedException();
        }
    }
}