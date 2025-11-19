using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services.Interfaces;
using System.Security.Claims;

namespace ABCRetailers.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ITableStorageService<Cart> _cartService;
        private readonly ITableStorageService<Products> _productService;
        private readonly ITableStorageService<Orders> _orderService;
        private readonly IQueueStorageService _queueService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ITableStorageService<Cart> cartService,
            ITableStorageService<Products> productService,
            ITableStorageService<Orders> orderService,
            IQueueStorageService queueService,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _productService = productService;
            _orderService = orderService;
            _queueService = queueService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private string GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        }

        private string GetCartPartitionKey()
        {
            return $"Cart_{GetCurrentUserId()}";
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            try
            {
                var cartItems = await _cartService.GetAllEntitiesAsync(GetCartPartitionKey());
                var viewModel = new CartViewModel();

                foreach (var cartItem in cartItems)
                {
                    var product = await _productService.GetEntityAsync("Products", cartItem.ProductId);
                    if (product != null)
                    {
                        viewModel.Items.Add(new CartItemViewModel
                        {
                            CartRowKey = cartItem.RowKey,
                            ProductId = cartItem.ProductId,
                            ProductName = product.Name,
                            Price = product.Price,
                            Quantity = cartItem.Quantity,
                            ImageUrl = product.ImageUrl
                        });
                    }
                    else
                    {
                        // Remove cart item if product no longer exists
                        await _cartService.DeleteEntityAsync(GetCartPartitionKey(), cartItem.RowKey);
                    }
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["ErrorMessage"] = "An error occurred while loading your cart.";
                return View(new CartViewModel());
            }
        }

        // POST: Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string productId, int quantity = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(productId) || quantity < 1)
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                var product = await _productService.GetEntityAsync("Products", productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (product.StockQuantity < quantity)
                {
                    return Json(new { success = false, message = $"Only {product.StockQuantity} items available" });
                }

                // Check if item already in cart
                var existingCartItems = await _cartService.GetAllEntitiesAsync(GetCartPartitionKey());
                var existingCartItem = existingCartItems.FirstOrDefault(c => c.ProductId == productId);

                if (existingCartItem != null)
                {
                    var newQuantity = existingCartItem.Quantity + quantity;
                    if (product.StockQuantity < newQuantity)
                    {
                        return Json(new { success = false, message = $"Cannot add more than available stock. You have {existingCartItem.Quantity} in cart, only {product.StockQuantity} available." });
                    }

                    existingCartItem.Quantity = newQuantity;
                    var updateSuccess = await _cartService.UpdateEntityAsync(existingCartItem);

                    if (updateSuccess)
                    {
                        return Json(new { success = true, message = "Cart updated successfully" });
                    }
                }
                else
                {
                    var cartItem = new Cart
                    {
                        PartitionKey = GetCartPartitionKey(),
                        RowKey = Guid.NewGuid().ToString(),
                        UserId = GetCurrentUserId(),
                        ProductId = productId,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = quantity,
                        ImageUrl = product.ImageUrl,
                        AddedDate = DateTime.UtcNow
                    };

                    var addSuccess = await _cartService.AddEntityAsync(cartItem);

                    if (addSuccess)
                    {
                        return Json(new { success = true, message = "Product added to cart successfully" });
                    }
                }

                return Json(new { success = false, message = "Failed to update cart" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return Json(new { success = false, message = "An error occurred while updating your cart" });
            }
        }

        // POST: Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string rowKey, int quantity)
        {
            try
            {
                if (quantity < 1)
                {
                    return await Remove(rowKey);
                }

                var cartItem = await _cartService.GetEntityAsync(GetCartPartitionKey(), rowKey);
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found" });
                }

                var product = await _productService.GetEntityAsync("Products", cartItem.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (product.StockQuantity < quantity)
                {
                    return Json(new { success = false, message = $"Only {product.StockQuantity} items available" });
                }

                cartItem.Quantity = quantity;
                var success = await _cartService.UpdateEntityAsync(cartItem);

                if (success)
                {
                    return Json(new { success = true, message = "Cart updated successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update cart" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart");
                return Json(new { success = false, message = "An error occurred while updating your cart" });
            }
        }

        // POST: Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(string rowKey)
        {
            try
            {
                var success = await _cartService.DeleteEntityAsync(GetCartPartitionKey(), rowKey);

                if (success)
                {
                    return Json(new { success = true, message = "Item removed from cart" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to remove item from cart" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new { success = false, message = "An error occurred while removing the item" });
            }
        }

        // GET: Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var cartItems = await _cartService.GetAllEntitiesAsync(GetCartPartitionKey());
                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty";
                    return RedirectToAction("Index");
                }

                var viewModel = new CartViewModel();
                decimal grandTotal = 0;

                foreach (var cartItem in cartItems)
                {
                    var product = await _productService.GetEntityAsync("Products", cartItem.ProductId);
                    if (product != null)
                    {
                        if (product.StockQuantity < cartItem.Quantity)
                        {
                            TempData["ErrorMessage"] = $"Insufficient stock for {product.Name}. Only {product.StockQuantity} available.";
                            return RedirectToAction("Index");
                        }

                        viewModel.Items.Add(new CartItemViewModel
                        {
                            CartRowKey = cartItem.RowKey,
                            ProductId = cartItem.ProductId,
                            ProductName = product.Name,
                            Price = product.Price,
                            Quantity = cartItem.Quantity,
                            ImageUrl = product.ImageUrl
                        });

                        grandTotal += cartItem.Price * cartItem.Quantity;
                    }
                }

                ViewBag.GrandTotal = grandTotal;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout");
                TempData["ErrorMessage"] = "An error occurred while loading checkout.";
                return RedirectToAction("Index");
            }
        }

        // POST: Cart/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder()
        {
            try
            {
                var cartItems = await _cartService.GetAllEntitiesAsync(GetCartPartitionKey());
                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty";
                    return RedirectToAction("Index");
                }

                var username = GetCurrentUsername();
                var userId = GetCurrentUserId();

                // Validate stock and create orders
                foreach (var cartItem in cartItems)
                {
                    var product = await _productService.GetEntityAsync("Products", cartItem.ProductId);
                    if (product == null)
                    {
                        TempData["ErrorMessage"] = $"Product {cartItem.ProductName} no longer exists";
                        return RedirectToAction("Index");
                    }

                    if (product.StockQuantity < cartItem.Quantity)
                    {
                        TempData["ErrorMessage"] = $"Insufficient stock for {product.Name}. Only {product.StockQuantity} available.";
                        return RedirectToAction("Index");
                    }

                    // Create order
                    var order = new Orders
                    {
                        PartitionKey = "Orders",
                        RowKey = Guid.NewGuid().ToString(),
                        CustomerId = userId.ToString(),
                        CustomerName = username,
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.ProductName,
                        Quantity = cartItem.Quantity,
                        TotalPrice = cartItem.Price * cartItem.Quantity,
                        Status = "Pending",
                        OrderDate = DateTime.UtcNow
                    };

                    // Add order to table storage
                    var orderSuccess = await _orderService.AddEntityAsync(order);
                    if (!orderSuccess)
                    {
                        TempData["ErrorMessage"] = "Failed to create order. Please try again.";
                        return RedirectToAction("Index");
                    }

                    // Send to queue for processing
                    var orderMessage = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        OrderId = order.RowKey,
                        CustomerId = order.CustomerId,
                        CustomerName = order.CustomerName,
                        ProductId = order.ProductId,
                        ProductName = order.ProductName,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate
                    });

                    var queueSuccess = await _queueService.SendMessageAsync(orderMessage, "orders");
                    if (!queueSuccess)
                    {
                        _logger.LogWarning("Failed to send order {OrderId} to queue, but order was created", order.RowKey);
                    }
                }

                // Clear cart after successful order creation
                foreach (var cartItem in cartItems)
                {
                    await _cartService.DeleteEntityAsync(GetCartPartitionKey(), cartItem.RowKey);
                }

                TempData["SuccessMessage"] = "Order placed successfully! It will be processed shortly.";
                return RedirectToAction("Confirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                TempData["ErrorMessage"] = "An error occurred while placing your order. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // GET: Cart/Confirmation
        public IActionResult Confirmation()
        {
            return View();
        }

        // GET: Cart/Count (for cart badge)
        [HttpGet]
        public async Task<JsonResult> Count()
        {
            try
            {
                var cartItems = await _cartService.GetAllEntitiesAsync(GetCartPartitionKey());
                var totalItems = cartItems.Sum(item => item.Quantity);
                return Json(new { count = totalItems });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { count = 0 });
            }
        }
    }
}