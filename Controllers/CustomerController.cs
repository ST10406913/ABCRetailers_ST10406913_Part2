using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;
        private readonly IAzureStorageService _azureStorageService;

        public CustomerController(ILogger<CustomerController> logger, IAzureStorageService azureStorageService)
        {
            _logger = logger;
            _azureStorageService = azureStorageService;
        }

        // -------------------------
        // Index
        // -------------------------
        public async Task<IActionResult> Index()
        {
            var customers = await _azureStorageService.GetAllEntitiesAsync<Customer>();
            return View(customers);
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
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _azureStorageService.AddEntityAsync(customer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // -------------------------
        // Edit (GET)
        // -------------------------
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _azureStorageService.GetEntityAsync<Customer>(partitionKey, rowKey);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // -------------------------
        // Edit (POST) — FIXED
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                // fetch the existing entity to preserve keys + ETag
                var existingCustomer = await _azureStorageService.GetEntityAsync<Customer>(customer.PartitionKey, customer.RowKey);
                if (existingCustomer == null)
                    return NotFound();

                // update properties
                existingCustomer.FirstName = customer.FirstName;
                existingCustomer.LastName = customer.LastName;
                existingCustomer.Email = customer.Email;
                existingCustomer.PhoneNumber = customer.PhoneNumber;

                await _azureStorageService.UpdateEntityAsync(existingCustomer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // -------------------------
        // Delete (GET)
        // -------------------------
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var customer = await _azureStorageService.GetEntityAsync<Customer>(partitionKey, rowKey);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // -------------------------
        // Delete (POST)
        // -------------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _azureStorageService.DeleteEntityAsync<Customer>(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}
