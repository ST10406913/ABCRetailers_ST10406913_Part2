// Services/Interfaces/ITableStorageService.cs
using ABCRetailers.Models;

namespace ABCRetailers.Services.Interfaces
{
    public interface ITableStorageService<T> where T : class
    {
        Task<T?> GetEntityAsync(string partitionKey, string rowKey);
        Task<IEnumerable<T>> GetAllEntitiesAsync(string? partitionKey = null);  // Changed to string?
        Task<bool> AddEntityAsync(T entity);
        Task<bool> UpdateEntityAsync(T entity);
        Task<bool> DeleteEntityAsync(string partitionKey, string rowKey);
        Task<IEnumerable<T>> SearchEntitiesAsync(string searchTerm, string? partitionKey = null);  // Changed to string?
    }
}