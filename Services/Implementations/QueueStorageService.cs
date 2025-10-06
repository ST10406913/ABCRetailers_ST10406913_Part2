using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.Models;

namespace ABCRetailers.Services.Implementations
{
    public class QueueStorageService : IQueueStorageService
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ILogger<QueueStorageService> _logger;
        private readonly AzureStorageSettings _settings;

        public QueueStorageService(IOptions<AzureStorageSettings> options, ILogger<QueueStorageService> logger)
        {
            _settings = options.Value;
            _logger = logger;

            try
            {
                _queueServiceClient = new QueueServiceClient(_settings.ConnectionString);
                InitializeQueuesAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Queue Storage Service");
                throw;
            }
        }

        private async Task InitializeQueuesAsync()
        {
            try
            {
                var queues = new[] { _settings.QueueNames.OrderQueue };

                foreach (var queueName in queues)
                {
                    var queueClient = _queueServiceClient.GetQueueClient(queueName);
                    await queueClient.CreateIfNotExistsAsync();
                    _logger.LogInformation("Queue {QueueName} initialized successfully", queueName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize queues");
                throw;
            }
        }

        public async Task<bool> SendMessageAsync(string message, string queueName)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    throw new ArgumentException("Message cannot be null or empty");
                }

                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                var response = await queueClient.SendMessageAsync(message);

                _logger.LogInformation("Message sent to queue {QueueName}. Message ID: {MessageId}",
                    queueName, response.Value.MessageId);

                return !string.IsNullOrEmpty(response.Value.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to queue {QueueName}", queueName);
                throw;
            }
        }

        public async Task<string?> GetMessageAsync(string queueName)
        {
            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                var messages = await queueClient.ReceiveMessagesAsync(1);

                if (messages.Value.Length == 0)
                {
                    return null;
                }

                var message = messages.Value[0];
                _logger.LogInformation("Message retrieved from queue {QueueName}. Message ID: {MessageId}",
                    queueName, message.MessageId);

                return message.MessageText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving message from queue {QueueName}", queueName);
                throw;
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string popReceipt, string queueName)
        {
            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                var response = await queueClient.DeleteMessageAsync(messageId, popReceipt);

                _logger.LogInformation("Message {MessageId} deleted from queue {QueueName}", messageId, queueName);
                return !response.IsError;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId} from queue {QueueName}", messageId, queueName);
                throw;
            }
        }

        public async Task<int> GetMessageCountAsync(string queueName)
        {
            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                var properties = await queueClient.GetPropertiesAsync();

                _logger.LogInformation("Queue {QueueName} has {Count} messages", queueName, properties.Value.ApproximateMessagesCount);
                return properties.Value.ApproximateMessagesCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message count from queue {QueueName}", queueName);
                throw;
            }
        }
    }
}