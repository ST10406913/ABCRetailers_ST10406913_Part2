using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ABCRetailers.Models;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.ViewModels;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITableStorageService<Customers> _customerService;
        private readonly ITableStorageService<Products> _productService;
        private readonly ITableStorageService<Orders> _orderService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ITableStorageService<Customers> customerService,
            ITableStorageService<Products> productService,
            ITableStorageService<Orders> orderService,
            ILogger<HomeController> logger)
        {
            _customerService = customerService;
            _productService = productService;
            _orderService = orderService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboard = new HomeViewModel();

                // FIXED: Remove partition key parameters - get ALL entities
                var customers = await _customerService.GetAllEntitiesAsync() ?? new List<Customers>();
                var products = await _productService.GetAllEntitiesAsync() ?? new List<Products>();
                var orders = await _orderService.GetAllEntitiesAsync() ?? new List<Orders>();

                dashboard.TotalCustomers = customers.Count();
                dashboard.TotalProducts = products.Count();
                dashboard.TotalOrders = orders.Count();
                dashboard.TotalRevenue = orders.Any() ? orders.Sum(o => o.TotalPrice) : 0;

                // Get recent orders for the table
                dashboard.RecentOrders = orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToList();

                // Get order status distribution
                dashboard.OrderStatusCounts = orders.Any()
                    ? orders.GroupBy(o => o.Status)
                           .ToDictionary(g => g.Key, g => g.Count())
                    : new Dictionary<string, int>();

                // Get low stock products
                dashboard.LowStockProducts = products.Any()
                    ? products.Where(p => p.StockQuantity < 10)
                             .OrderBy(p => p.StockQuantity)
                             .Take(5)
                             .ToList()
                    : new List<Products>();

                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                // Return empty dashboard on error
                return View(new HomeViewModel());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}