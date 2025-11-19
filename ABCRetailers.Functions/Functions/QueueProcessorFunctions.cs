using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.Models;
using System.Text.Json;

namespace ABCRetailers.Functions
{
    public class QueueProcessorFunctions
    {
        private readonly IOrdersService _ordersService;
        private readonly ILogger<QueueProcessorFunctions> _logger;

        public QueueProcessorFunctions(ILogger<QueueProcessorFunctions> logger, IOrdersService ordersService)
        {
            _logger = logger;
            _ordersService = ordersService;
        }

        [Function("ProcessOrderQueue")]
        public async Task Run(
            [QueueTrigger("ordersqueue", Connection = "AzureWebJobsStorage")] string queueMessage)
        {
            _logger.LogInformation($"Processing order from queue: {queueMessage}");

            try
            {
                var order = JsonSerializer.Deserialize<Orders>(queueMessage);

                if (order == null)
                {
                    _logger.LogError("Failed to deserialize order from queue message");
                    return;
                }

                // Use the OrdersService to create the order
                var success = await _ordersService.CreateOrderAsync(order);

                if (success)
                {
                    _logger.LogInformation($"Order {order.RowKey} successfully processed from queue.");
                }
                else
                {
                    _logger.LogError($"Failed to process order {order.RowKey} from queue.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order from queue.");
            }
        }
    }
}