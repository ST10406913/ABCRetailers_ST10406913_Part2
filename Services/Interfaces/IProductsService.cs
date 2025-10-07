using ABCRetailers.Models;

namespace ABCRetailers.Services.Interfaces
{
    public interface IProductsService
    {
        Task<Products?> GetProductAsync(string productId);
        Task<IEnumerable<Products>> GetAllProductsAsync();
        Task<bool> CreateProductAsync(Products product);
        Task<IEnumerable<Products>> SearchProductsAsync(string searchTerm);
    }
}