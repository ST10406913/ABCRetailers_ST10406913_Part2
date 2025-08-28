using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // -------------------------
        // Index
        // -------------------------
        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        // -------------------------
        // Create (GET)
        // -------------------------
        public IActionResult Create()
        {
            return View();
        }

        // -------------------------
        // Create (POST)
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            try
            {
                if (Request.Form.TryGetValue("Price", out var priceFormValue))
                {
                    _logger.LogInformation("Raw price from form: {Price}", priceFormValue.ToString());

                    if (decimal.TryParse(priceFormValue, out var parsedPrice))
                    {
                        product.Price = (double)parsedPrice;
                        _logger.LogInformation("Successfully parsed price: {Price}", parsedPrice);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse price: {Price}", priceFormValue.ToString());
                    }
                }

                _logger.LogInformation("Final product price: {Price}", product.Price);

                if (product.Price <= 0)
                {
                    ModelState.AddModelError("Price", "Price must be greater than $0.00");
                    return View(product);
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                    product.ImageUrl = imageUrl;
                }

                await _storageService.AddEntityAsync(product);
                TempData["Success"] = $"Product '{product.ProductName}' created successfully with price {product.Price:C}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {Message}", ex.Message);
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                return View(product);
            }
        }

        // -------------------------
        // Edit (GET)
        // -------------------------
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _storageService.GetEntityAsync<Product>("Product", id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // -------------------------
        // Edit (POST)
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            try
            {
                if (Request.Form.TryGetValue("Price", out var priceFormValue) &&
                    decimal.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = (double)parsedPrice;
                    _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);
                }

                if (ModelState.IsValid)
                {
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                        return NotFound();

                    // Update fields but preserve original ETag
                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.StockAvailable = product.StockAvailable;
                    originalProduct.Price = product.Price;

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        originalProduct.ImageUrl = imageUrl;
                    }

                    await _storageService.UpdateEntityAsync(originalProduct);
                    TempData["Success"] = "Product updated successfully!";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
            }

            return View(product);
        }

        // -------------------------
        // Delete
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Product>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
