using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ABCRetailers.Models;
using ABCRetailers.Services.Interfaces;

namespace ABCRetailers.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ITableStorageService<Customers> _customerService;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ITableStorageService<Customers> customerService, ILogger<CustomersController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        // GET: Customers with search functionality
        public async Task<IActionResult> Index(string searchString)
        {
            try
            {
                ViewData["CurrentFilter"] = searchString;

                IEnumerable<Customers> customers;

                if (!string.IsNullOrEmpty(searchString))
                {
                    customers = await _customerService.SearchEntitiesAsync(searchString);
                    ViewData["SearchResults"] = $"{customers.Count()} customers found for \"{searchString}\"";
                }
                else
                {
                    customers = await _customerService.GetAllEntitiesAsync();
                }

                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                TempData["ErrorMessage"] = "An error occurred while retrieving customers.";
                return View(new List<Customers>());
            }
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    return NotFound();
                }

                var customer = await _customerService.GetEntityAsync(partitionKey, rowKey);
                if (customer == null)
                {
                    return NotFound();
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer details");
                TempData["ErrorMessage"] = "An error occurred while retrieving customer details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,Phone,Address")] Customers customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // CORRECT: Already using "Customer" (singular)
                    customer.PartitionKey = "Customer";
                    customer.RowKey = Guid.NewGuid().ToString();

                    var success = await _customerService.AddEntityAsync(customer);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Customer created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create customer. Please try again.";
                    }
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                TempData["ErrorMessage"] = "An error occurred while creating the customer.";
                return View(customer);
            }
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    return NotFound();
                }

                var customer = await _customerService.GetEntityAsync(partitionKey, rowKey);
                if (customer == null)
                {
                    return NotFound();
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer for edit");
                TempData["ErrorMessage"] = "An error occurred while retrieving the customer.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, [Bind("PartitionKey,RowKey,FirstName,LastName,Email,Phone,Address,CreatedDate")] Customers customer)
        {
            try
            {
                if (partitionKey != customer.PartitionKey || rowKey != customer.RowKey)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    var success = await _customerService.UpdateEntityAsync(customer);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Customer updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update customer. Please try again.";
                    }
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer");
                TempData["ErrorMessage"] = "An error occurred while updating the customer.";
                return View(customer);
            }
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            try
            {
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    return NotFound();
                }

                var customer = await _customerService.GetEntityAsync(partitionKey, rowKey);
                if (customer == null)
                {
                    return NotFound();
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer for delete");
                TempData["ErrorMessage"] = "An error occurred while retrieving the customer.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            try
            {
                var success = await _customerService.DeleteEntityAsync(partitionKey, rowKey);
                if (success)
                {
                    TempData["SuccessMessage"] = "Customer deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete customer. Please try again.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer");
                TempData["ErrorMessage"] = "An error occurred while deleting the customer.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}