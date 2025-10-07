using ABCRetailers.Models;
using ABCRetailers.Services.Interfaces;

namespace ABCRetailers.Services.Implementations
{
    public class ProductsService : IProductsService
    {
        private readonly ITableStorageService<Products> _tableService;

        public ProductsService(ITableStorageService<Products> tableService)
        {
            _tableService = tableService;
        }

        public async Task<Products?> GetProductAsync(string productId)
        {
            return await _tableService.GetEntityAsync("Products", productId);
        }

        public async Task<IEnumerable<Products>> GetAllProductsAsync()
        {
            return await _tableService.GetAllEntitiesAsync("Products");
        }

        public async Task<bool> CreateProductAsync(Products product)
        {
            product.PartitionKey = "Products";
            if (string.IsNullOrEmpty(product.RowKey))
                product.RowKey = Guid.NewGuid().ToString();

            return await _tableService.AddEntityAsync(product);
        }

        public async Task<IEnumerable<Products>> SearchProductsAsync(string searchTerm)
        {
            return await _tableService.SearchEntitiesAsync(searchTerm, "Products");
        }
    }
}