using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using ABCRetailers.Models;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.ViewModels;

namespace ABCRetailers.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ITableStorageService<Orders> _orderService;
        private readonly ITableStorageService<Customers> _customerService;
        private readonly ITableStorageService<Products> _productService;
        private readonly IQueueStorageService _queueService;
        private readonly ILogger<OrdersController> _logger;
        private readonly AzureStorageSettings _settings;

        public OrdersController(
            ITableStorageService<Orders> orderService,
            ITableStorageService<Customers> customerService,
            ITableStorageService<Products> productService,
            IQueueStorageService queueService,
            IOptions<AzureStorageSettings> options,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _customerService = customerService;
            _productService = productService;
            _queueService = queueService;
            _logger = logger;
            _settings = options.Value;
        }

        // GET: Orders with search and status filter
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            try
            {
                ViewData["CurrentFilter"] = searchString;
                ViewData["StatusFilter"] = statusFilter;

                IEnumerable<Orders> orders;

                if (!string.IsNullOrEmpty(searchString))
                {
                    orders = await _orderService.SearchEntitiesAsync(searchString);
                    ViewData["SearchResults"] = $"{orders.Count()} orders found for \"{searchString}\"";
                }
                else
                {
                    orders = await _orderService.GetAllEntitiesAsync();
                }

                // Apply status filter if selected
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    orders = orders.Where(o => o.Status == statusFilter);
                }

                // Get statuses for filter dropdown
                ViewBag.Statuses = new[] { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };

                return View(orders.OrderByDescending(o => o.OrderDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                TempData["ErrorMessage"] = "An error occurred while retrieving orders.";
                return View(new List<Orders>());
            }
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                // FIXED: Get all entities without partition key
                var customers = await _customerService.GetAllEntitiesAsync() ?? new List<Customers>();
                var products = await _productService.GetAllEntitiesAsync() ?? new List<Products>();

                var viewModel = new OrderCreateViewModel
                {
                    // FIXED: Convert to SelectListItem with proper display text
                    CustomerOptions = customers.Select(c => new SelectListItem
                    {
                        Value = c.RowKey,
                        Text = $"{c.FirstName} {c.LastName} - {c.Email}"
                    }).ToList(),

                    ProductOptions = products.Select(p => new SelectListItem
                    {
                        Value = p.RowKey,
                        Text = $"{p.Name} - R{p.Price:0.00}"
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order creation form");
                TempData["ErrorMessage"] = "An error occurred while loading the order form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Get customer and product details
                    var customer = await _customerService.GetEntityAsync("Customer", viewModel.CustomerId);
                    var product = await _productService.GetEntityAsync("Product", viewModel.ProductId);

                    if (customer == null || product == null)
                    {
                        TempData["ErrorMessage"] = "Invalid customer or product selected.";
                        return await Create();
                    }

                    if (product.StockQuantity < viewModel.Quantity)
                    {
                        TempData["ErrorMessage"] = $"Insufficient stock. Only {product.StockQuantity} items available.";
                        return await Create();
                    }

                    // Create order
                    var order = new Orders
                    {
                        PartitionKey = "Order",
                        RowKey = Guid.NewGuid().ToString(),
                        CustomerId = viewModel.CustomerId,
                        ProductId = viewModel.ProductId,
                        CustomerName = $"{customer.FirstName} {customer.LastName}",
                        ProductName = product.Name,
                        Quantity = viewModel.Quantity,
                        TotalPrice = product.Price * viewModel.Quantity,
                        Status = viewModel.Status,
                        OrderDate = viewModel.OrderDate
                    };

                    // Send order to queue instead of direct table storage
                    var orderMessage = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        OrderId = order.RowKey,
                        CustomerId = order.CustomerId,
                        ProductId = order.ProductId,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate
                    });

                    var success = await _queueService.SendMessageAsync(orderMessage, _settings.QueueNames.OrderQueue);

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Order placed successfully! It will be processed shortly.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to place order. Please try again.";
                    }
                }

                // FIXED: Reload dropdown data properly if validation fails
                var customers = await _customerService.GetAllEntitiesAsync() ?? new List<Customers>();
                var products = await _productService.GetAllEntitiesAsync() ?? new List<Products>();

                viewModel.CustomerOptions = customers.Select(c => new SelectListItem
                {
                    Value = c.RowKey,
                    Text = $"{c.FirstName} {c.LastName} - {c.Email}"
                }).ToList();

                viewModel.ProductOptions = products.Select(p => new SelectListItem
                {
                    Value = p.RowKey,
                    Text = $"{p.Name} - R{p.Price:0.00}"
                }).ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["ErrorMessage"] = "An error occurred while placing the order.";
                return await Create();
            }
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    return NotFound();
                }

                var order = await _orderService.GetEntityAsync(partitionKey, rowKey);
                if (order == null)
                {
                    return NotFound();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details");
                TempData["ErrorMessage"] = "An error occurred while retrieving order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Orders/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string partitionKey, string rowKey, string status)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey) || string.IsNullOrEmpty(status))
                {
                    return Json(new { success = false, message = "Invalid parameters" });
                }

                var order = await _orderService.GetEntityAsync(partitionKey, rowKey);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                order.Status = status;

                // Update dates based on status
                if (status == "Shipped")
                {
                    order.ShippedDate = DateTime.UtcNow;
                }
                else if (status == "Delivered")
                {
                    order.DeliveredDate = DateTime.UtcNow;
                }

                var success = await _orderService.UpdateEntityAsync(order);

                if (success)
                {
                    return Json(new { success = true, message = $"Order status updated to {status}" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update order status" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return Json(new { success = false, message = "An error occurred while updating order status" });
            }
        }

        // GET: Orders/Search - API endpoint for AJAX search
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            try
            {
                if (string.IsNullOrEmpty(term))
                {
                    return Json(new { results = new List<object>() });
                }

                var orders = await _orderService.SearchEntitiesAsync(term);
                var results = orders.Select(o => new
                {
                    id = o.RowKey,
                    text = $"{o.CustomerName} - {o.ProductName} - {o.Status} - {o.OrderDate:yyyy-MM-dd}"
                });

                return Json(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching orders");
                return Json(new { results = new List<object>() });
            }
        }
    }
}