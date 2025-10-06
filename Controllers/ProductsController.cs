// Controllers/ProductsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ABCRetailers.Models;
using ABCRetailers.Services.Interfaces;

namespace ABCRetailers.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ITableStorageService<Products> _productService;
        private readonly IBlobStorageService _blobService;
        private readonly ILogger<ProductsController> _logger;
        private readonly AzureStorageSettings _settings;

        public ProductsController(
            ITableStorageService<Products> productService,
            IBlobStorageService blobService,
            IOptions<AzureStorageSettings> options,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _blobService = blobService;
            _logger = logger;
            _settings = options.Value;
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
                    products = await _productService.SearchEntitiesAsync(searchString, "Products");
                    ViewData["SearchResults"] = $"{products.Count()} products found for \"{searchString}\"";
                }
                else
                {
                    products = await _productService.GetAllEntitiesAsync("Products");
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
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Category,StockQuantity")] Products product, IFormFile imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    product.PartitionKey = "Products";
                    product.RowKey = Guid.NewGuid().ToString();

                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _blobService.UploadFileAsync(imageFile, _settings.BlobContainerNames.ProductImages);
                        product.ImageUrl = imageUrl;
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
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, [Bind("PartitionKey,RowKey,Name,Description,Price,Category,StockQuantity,ImageUrl,CreatedDate")] Products product, IFormFile imageFile)
        {
            try
            {
                if (partitionKey != product.PartitionKey || rowKey != product.RowKey)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    // Handle image upload if new file provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _blobService.UploadFileAsync(imageFile, _settings.BlobContainerNames.ProductImages);
                        product.ImageUrl = imageUrl;
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