using ABCRetailers.Models;
using ABCRetailers.Services.Interfaces;

namespace ABCRetailers.Services.Implementations
{
    public class OrdersService : IOrdersService
    {
        private readonly ITableStorageService<Orders> _tableService;

        public OrdersService(ITableStorageService<Orders> tableService)
        {
            _tableService = tableService;
        }

        public async Task<Orders?> GetOrderAsync(string orderId)
        {
            return await _tableService.GetEntityAsync("Orders", orderId);
        }

        public async Task<IEnumerable<Orders>> GetAllOrdersAsync()
        {
            return await _tableService.GetAllEntitiesAsync("Orders");
        }

        public async Task<bool> CreateOrderAsync(Orders order)
        {
            order.PartitionKey = "Orders";
            if (string.IsNullOrEmpty(order.RowKey))
                order.RowKey = Guid.NewGuid().ToString();

            return await _tableService.AddEntityAsync(order);
        }

        public async Task<IEnumerable<Orders>> SearchOrdersAsync(string searchTerm)
        {
            return await _tableService.SearchEntitiesAsync(searchTerm, "Orders");
        }
    }
}