using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IAzureStorageService storageService, ILogger<OrderController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // -------------------------
        // Index
        // -------------------------
        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            return View(orders);
        }

        // -------------------------
        // Create (GET)
        // -------------------------
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // -------------------------
        // Create (POST)
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    order.OrderDate = DateTime.UtcNow;
                    order.Status = "Submitted";

                    await _storageService.AddEntityAsync(order);
                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order: {Message}", ex.Message);
                ModelState.AddModelError("", $"Error creating order: {ex.Message}");
            }

            await PopulateDropdowns();
            return View(order);
        }

        // -------------------------
        // Details
        // -------------------------
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // -------------------------
        // Edit
        // -------------------------
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", id);

            if (order == null)
                return NotFound();

            await PopulateDropdowns();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var originalOrder = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);
                    if (originalOrder == null)
                        return NotFound();

                    originalOrder.CustomerId = order.CustomerId;
                    originalOrder.ProductId = order.ProductId;
                    originalOrder.Quantity = order.Quantity;
                    originalOrder.Status = order.Status;

                    await _storageService.UpdateEntityAsync(originalOrder);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {Message}", ex.Message);
                ModelState.AddModelError("", $"Error updating order: {ex.Message}");
            }

            await PopulateDropdowns();
            return View(order);
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
                await _storageService.DeleteEntityAsync<Order>("Order", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // -------------------------
        // GetProductPrice
        // -------------------------
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return Json(0);

            var product = await _storageService.GetEntityAsync<Product>("Product", productId);
            return Json(product?.Price ?? 0);
        }

        // -------------------------
        // UploadOrderStatus
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadOrderStatus(string id, string status)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order == null)
                    return NotFound();

                order.Status = status;
                await _storageService.UpdateEntityAsync(order);

                TempData["Success"] = "Order status updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating order status: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // -------------------------
        // Populate Dropdowns
        // -------------------------
        private async Task PopulateDropdowns()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

            ViewBag.Customers = new SelectList(customers, "RowKey", "Name");
            ViewBag.Products = new SelectList(products, "RowKey", "ProductName");
        }
    }
}
