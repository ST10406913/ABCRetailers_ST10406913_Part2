using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IAzureStorageService storageService, ILogger<UploadController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // -------------------------
        // Index
        // -------------------------
        public IActionResult Index()
        {
            return View();
        }

        // -------------------------
        // Upload Image
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "No file selected for upload.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var imageUrl = await _storageService.UploadImageAsync(file, "uploads");
                TempData["Success"] = $"Image uploaded successfully! URL: {imageUrl}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image: {Message}", ex.Message);
                TempData["Error"] = $"Error uploading image: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // -------------------------
        // Upload File
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "No file selected for upload.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var fileUrl = await _storageService.UploadFileAsync(file, "uploads");
                TempData["Success"] = $"File uploaded successfully! URL: {fileUrl}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {Message}", ex.Message);
                TempData["Error"] = $"Error uploading file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // -------------------------
        // Upload to File Share
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadToFileShare(IFormFile file, string directoryName = "")
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "No file selected for upload.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var filePath = await _storageService.UploadToFileShareAsync(file, "uploads", directoryName);
                TempData["Success"] = $"File uploaded to file share successfully! Path: {filePath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to file share: {Message}", ex.Message);
                TempData["Error"] = $"Error uploading to file share: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}