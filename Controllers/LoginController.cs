using System.Security.Claims;
using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly ITableStorageService<Customers> _customerService;
        private readonly ILogger<LoginController> _logger;

        public LoginController(
            AuthDbContext context,
            ITableStorageService<Customers> customerService,
            ILogger<LoginController> logger)
        {
            _context = context;
            _customerService = customerService;
            _logger = logger;
        }

        // GET: Login
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username);

                    if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                    {
                        // Update last login
                        user.LastLogin = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        // Create claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.Role, user.Role)
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";

                        // Redirect based on role
                        return user.Role == "Admin"
                            ? RedirectToAction("Index", "Home")
                            : RedirectToAction("Index", "Products");
                    }

                    ModelState.AddModelError(string.Empty, "Invalid username or password");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                TempData["ErrorMessage"] = "An error occurred during login.";
                return View(model);
            }
        }

        // GET: Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if username or email already exists
                    if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "Username already exists");
                        return View(model);
                    }

                    if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Email already exists");
                        return View(model);
                    }

                    // Create Azure Table Customer
                    var customer = new Customers
                    {
                        PartitionKey = "Customers",
                        RowKey = Guid.NewGuid().ToString(),
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        Phone = model.Phone,
                        Address = model.Address,
                        CreatedDate = DateTime.UtcNow
                    };

                    var customerSuccess = await _customerService.AddEntityAsync(customer);
                    if (!customerSuccess)
                    {
                        TempData["ErrorMessage"] = "Failed to create customer record.";
                        return View(model);
                    }

                    // Create User in SQL Database
                    var user = new User
                    {
                        Username = model.Username,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        Email = model.Email,
                        Role = "Customer",
                        CustomerRowKey = customer.RowKey,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Registration successful! Please login.";
                    return RedirectToAction("Login");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                TempData["ErrorMessage"] = "An error occurred during registration.";
                return View(model);
            }
        }

        // POST: Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Access Denied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}