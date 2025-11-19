using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetailers.Functions
{
    public class ProductsFunctions
    {
        private readonly IProductsService _productsService;
        private readonly ILogger<ProductsFunctions> _logger;

        public ProductsFunctions(ILogger<ProductsFunctions> logger, IProductsService productsService)
        {
            _logger = logger;
            _productsService = productsService;
        }

        [Function("SearchProducts")]
        public async Task<IActionResult> SearchProducts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/search")] HttpRequest req)
        {
            _logger.LogInformation("Searching products...");

            // Fixed: Handle potential null value from Query
            string searchTerm = req.Query["term"].ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(searchTerm))
                return new BadRequestObjectResult("Search term is required.");

            var products = await _productsService.SearchProductsAsync(searchTerm);
            return new OkObjectResult(products);
        }

        [Function("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequest req)
        {
            _logger.LogInformation("Getting all products...");

            var products = await _productsService.GetAllProductsAsync();
            return new OkObjectResult(products);
        }

        [Function("GetProduct")]
        public async Task<IActionResult> GetProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogInformation($"Getting product {id}...");

            // Fixed: Added null/empty check for ID
            if (string.IsNullOrEmpty(id))
                return new BadRequestObjectResult("Product ID is required.");

            var product = await _productsService.GetProductAsync(id);
            return product != null ? new OkObjectResult(product) : new NotFoundResult();
        }
    }
}