using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ABCRetailers.Models;
using ABCRetailers.Services.Interfaces;
using System.Net.Http; // Added for HttpClient

namespace ABCRetailers.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ITableStorageService<Products> _productService;
        private readonly IBlobStorageService _blobService; // Still used for read operations if needed
        private readonly ILogger<ProductsController> _logger;
        private readonly AzureStorageSettings _settings;
        private readonly HttpClient _httpClient; // Best practice: Inject HttpClient

        public ProductsController(
            ITableStorageService<Products> productService,
            IBlobStorageService blobService,
            IOptions<AzureStorageSettings> options,
            ILogger<ProductsController> logger,
            IHttpClientFactory httpClientFactory) // Inject Factory
        {
            _productService = productService;
            _blobService = blobService;
            _logger = logger;
            _settings = options.Value;
            _httpClient = httpClientFactory.CreateClient(); // Create client
        }

        // GET: Products with search functionality
        public async Task<IActionResult> Index(string searchString, string categoryFilter)
        {
            try
            {
                ViewData["CurrentFilter"] = searchString;
                ViewData["CategoryFilter"] = categoryFilter;

                IEnumerable<Products> products;

                if (!string.IsNullOrEmpty(searchString))
                {
                    products = await _productService.SearchEntitiesAsync(searchString);
                    ViewData["SearchResults"] = $"{products.Count()} products found for \"{searchString}\"";
                }
                else
                {
                    products = await _productService.GetAllEntitiesAsync();
                }

                // Apply category filter if selected
                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    products = products.Where(p => p.Category == categoryFilter);
                }

                // Get unique categories for filter dropdown
                ViewBag.Categories = products.Select(p => p.Category).Distinct().OrderBy(c => c);

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                TempData["ErrorMessage"] = "An error occurred while retrieving products.";
                return View(new List<Products>());
            }
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    return NotFound();
                }

                var product = await _productService.GetEntityAsync(partitionKey, rowKey);
                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product details");
                TempData["ErrorMessage"] = "An error occurred while retrieving product details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Products product, IFormFile imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    product.PartitionKey = "Product";
                    product.RowKey = Guid.NewGuid().ToString();
                    product.CreatedDate = DateTime.UtcNow;

                    // Handle image upload via Azure Function
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Upload via Function
                        using (var content = new MultipartFormDataContent())
                        {
                            content.Add(new StreamContent(imageFile.OpenReadStream()), "file", imageFile.FileName);
                            
                            // Function URL (Ensure port 7071 is correct from your console)
                            var response = await _httpClient.PostAsync("http://localhost:7071/api/UploadBlob", content);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                // Construct the URL manually since the function just uploads it
                                // Format: https://[account].blob.core.windows.net/[container]/[filename]
                                // You might want to make the Function return the URL string to be safer
                                product.ImageUrl = $"https://{_settings.StorageAccountName}.blob.core.windows.net/{_settings.BlobContainerNames.ProductImages}/{imageFile.FileName}";
                            }
                            else
                            {
                                _logger.LogWarning("Function upload failed: " + response.ReasonPhrase);
                                // Fallback: Use local service if function fails (optional)
                                var imageUrl = await _blobService.UploadFileAsync(imageFile, _settings.BlobContainerNames.ProductImages);
                                product.ImageUrl = imageUrl;
                            }
                        }
                    }

                    var success = await _productService.AddEntityAsync(product);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Product created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create product. Please try again.";
                    }
                }
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                TempData["ErrorMessage"] = "An error occurred while creating the product.";
                return View(product);
            }
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    return NotFound();
                }

                var product = await _productService.GetEntityAsync(partitionKey, rowKey);
                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product for edit");
                TempData["ErrorMessage"] = "An error occurred while retrieving the product.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, Products product, IFormFile imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = await _productService.GetEntityAsync(product.PartitionKey, product.RowKey);
                    if (existingProduct == null)
                    {
                        TempData["ErrorMessage"] = "Product not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    product.CreatedDate = existingProduct.CreatedDate;

                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                         // Upload via Function
                        using (var content = new MultipartFormDataContent())
                        {
                            content.Add(new StreamContent(imageFile.OpenReadStream()), "file", imageFile.FileName);
                            
                            var response = await _httpClient.PostAsync("http://localhost:7071/api/UploadBlob", content);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                product.ImageUrl = $"https://{_settings.StorageAccountName}.blob.core.windows.net/{_settings.BlobContainerNames.ProductImages}/{imageFile.FileName}";
                            }
                            else 
                            {
                                 // Fallback
                                 var imageUrl = await _blobService.UploadFileAsync(imageFile, _settings.BlobContainerNames.ProductImages);
                                 product.ImageUrl = imageUrl;
                            }
                        }
                    }
                    else
                    {
                        product.ImageUrl = existingProduct.ImageUrl;
                    }

                    var success = await _productService.UpdateEntityAsync(product);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Product updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update product. Please try again.";
                    }
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                TempData["ErrorMessage"] = "An error occurred while updating the product.";
                return View(product);
            }
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    return NotFound();
                }

                var product = await _productService.GetEntityAsync(partitionKey, rowKey);
                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product for delete");
                TempData["ErrorMessage"] = "An error occurred while retrieving the product.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            try
            {
                var success = await _productService.DeleteEntityAsync(partitionKey, rowKey);
                if (success)
                {
                    TempData["SuccessMessage"] = "Product deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete product. Please try again.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                TempData["ErrorMessage"] = "An error occurred while deleting the product.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}