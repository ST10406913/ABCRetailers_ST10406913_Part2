using ABCRetailers.Models;

namespace ABCRetailers.Services.Interfaces
{
    public interface IOrdersService
    {
        Task<Orders?> GetOrderAsync(string orderId);
        Task<IEnumerable<Orders>> GetAllOrdersAsync();
        Task<bool> CreateOrderAsync(Orders order);
        Task<IEnumerable<Orders>> SearchOrdersAsync(string searchTerm);
    }
}