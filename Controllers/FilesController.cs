// Controllers/FilesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.Models;

namespace ABCRetailers.Controllers
{
    public class FilesController : Controller
    {
        private readonly IFileStorageService _fileService;
        private readonly IBlobStorageService _blobService;
        private readonly ILogger<FilesController> _logger;
        private readonly AzureStorageSettings _settings;

        public FilesController(
            IFileStorageService fileService,
            IBlobStorageService blobService,
            IOptions<AzureStorageSettings> options,
            ILogger<FilesController> logger)
        {
            _fileService = fileService;
            _blobService = blobService;
            _logger = logger;
            _settings = options.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: Files/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Files/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(FileUploadModel model)
        {
            try
            {
                if (model.File == null || model.File.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a file to upload.";
                    return View();
                }

                bool success;
                string? fileUrl = null;

                if (model.StorageType == "blob")
                {
                    // Upload to Blob Storage
                    fileUrl = await _blobService.UploadFileAsync(model.File, _settings.BlobContainerNames.Documents);
                    success = !string.IsNullOrEmpty(fileUrl);
                }
                else
                {
                    // Upload to File Storage
                    success = await _fileService.UploadFileAsync(model.File, _settings.FileShareNames.Documents, "uploads");
                }

                if (success)
                {
                    TempData["SuccessMessage"] = $"File '{model.File.FileName}' uploaded successfully to {model.StorageType} storage!";

                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        TempData["FileUrl"] = fileUrl;
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to upload file. Please try again.";
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                TempData["ErrorMessage"] = $"An error occurred while uploading the file: {ex.Message}";
                return View();
            }
        }

        // GET: Files/List
        public async Task<IActionResult> List()
        {
            try
            {
                var blobFiles = await _blobService.GetBlobListAsync(_settings.BlobContainerNames.Documents);
                var fileShareFiles = await _fileService.GetFileListAsync(_settings.FileShareNames.Documents, "uploads");

                var model = new FileListViewModel
                {
                    BlobFiles = blobFiles,
                    FileShareFiles = fileShareFiles
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file list");
                TempData["ErrorMessage"] = "An error occurred while retrieving files.";
                return View(new FileListViewModel());
            }
        }

        // GET: Files/Download
        public async Task<IActionResult> Download(string fileName, string storageType)
        {
            try
            {
                Stream fileStream;
                string contentType;

                if (storageType == "blob")
                {
                    fileStream = await _blobService.DownloadFileAsync(fileName, _settings.BlobContainerNames.Documents);
                    contentType = "application/octet-stream";
                }
                else
                {
                    fileStream = await _fileService.DownloadFileAsync(fileName, _settings.FileShareNames.Documents, "uploads");
                    contentType = "application/octet-stream";
                }

                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                TempData["ErrorMessage"] = "An error occurred while downloading the file.";
                return RedirectToAction(nameof(List));
            }
        }
    }
}