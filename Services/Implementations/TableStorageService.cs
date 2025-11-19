using ABCRetailers.Models;
using ABCRetailers.Services.Interfaces;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ABCRetailers.Services.Implementations
{
    public class TableStorageService<T> : ITableStorageService<T> where T : class, ITableEntity, new()
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<TableStorageService<T>> _logger;
        private readonly AzureStorageSettings _settings;

        public TableStorageService(IOptions<AzureStorageSettings> options, ILogger<TableStorageService<T>> logger)
        {
            _settings = options.Value;
            _logger = logger;

            try
            {
                _tableServiceClient = new TableServiceClient(_settings.ConnectionString);
                InitializeTableAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Table Storage Service");
                throw;
            }
        }

        private async Task InitializeTableAsync()
        {
            try
            {
                var tableName = GetTableName();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.CreateIfNotExistsAsync();
                _logger.LogInformation("Table {TableName} initialized successfully", tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize table");
                throw;
            }
        }

        private string GetTableName()
        {
            var type = typeof(T);
            return type.Name switch
            {
                nameof(Customers) => _settings.TableNames.Customers,
                nameof(Products) => _settings.TableNames.Products,
                nameof(Orders) => _settings.TableNames.Orders,
                nameof(Cart) => _settings.TableNames.Cart, // ADDED CART SUPPORT
                _ => typeof(T).Name.ToLower() + "s"
            };
        }

        public async Task<T?> GetEntityAsync(string partitionKey, string rowKey)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    throw new ArgumentException("PartitionKey and RowKey cannot be null or empty");
                }

                var tableClient = _tableServiceClient.GetTableClient(GetTableName());
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Entity not found - PartitionKey: {PartitionKey}, RowKey: {RowKey}", partitionKey, rowKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity - PartitionKey: {PartitionKey}, RowKey: {RowKey}", partitionKey, rowKey);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllEntitiesAsync(string? partitionKey = null)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient(GetTableName());
                var query = partitionKey == null
                    ? tableClient.QueryAsync<T>()
                    : tableClient.QueryAsync<T>(e => e.PartitionKey == partitionKey);

                var entities = new List<T>();
                await foreach (var entity in query)
                {
                    entities.Add(entity);
                }
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all entities for partition: {PartitionKey}", partitionKey);
                throw;
            }
        }

        public async Task<bool> AddEntityAsync(T entity)
        {
            try
            {
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(entity));
                }

                var tableClient = _tableServiceClient.GetTableClient(GetTableName());
                var response = await tableClient.AddEntityAsync(entity);
                _logger.LogInformation("Entity added successfully - PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    entity.PartitionKey, entity.RowKey);
                return !response.IsError;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 409)
            {
                _logger.LogWarning("Entity already exists - PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    entity.PartitionKey, entity.RowKey);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding entity - PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    entity.PartitionKey, entity.RowKey);
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(T entity)
        {
            try
            {
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(entity));
                }

                var tableClient = _tableServiceClient.GetTableClient(GetTableName());

                // FIXED: Use ETag.All to force update regardless of ETag
                var response = await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);

                _logger.LogInformation("Entity updated successfully - PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    entity.PartitionKey, entity.RowKey);
                return !response.IsError;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity - PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    entity.PartitionKey, entity.RowKey);
                throw;
            }
        }

        public async Task<bool> DeleteEntityAsync(string partitionKey, string rowKey)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    throw new ArgumentException("PartitionKey and RowKey cannot be null or empty");
                }

                var tableClient = _tableServiceClient.GetTableClient(GetTableName());
                var response = await tableClient.DeleteEntityAsync(partitionKey, rowKey);
                _logger.LogInformation("Entity deleted successfully - PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    partitionKey, rowKey);
                return !response.IsError;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity - PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    partitionKey, rowKey);
                throw;
            }
        }

        public async Task<IEnumerable<T>> SearchEntitiesAsync(string searchTerm, string? partitionKey = null)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    return await GetAllEntitiesAsync(partitionKey);
                }

                var allEntities = await GetAllEntitiesAsync(partitionKey);
                return allEntities.Where(entity =>
                    entity.GetType().GetProperties()
                        .Any(prop =>
                            prop.GetValue(entity)?.ToString()?
                            .Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true
                        )
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching entities with term: {SearchTerm}", searchTerm);
                throw;
            }
        }
    }
}