using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCRetailers.Services.Interfaces;
using System.Threading.Tasks;
using System.IO;

namespace ABCRetailers.Functions
{
    public class BlobFunctions
    {
        private readonly IBlobStorageService _blobService;
        private readonly ILogger<BlobFunctions> _logger;

        public BlobFunctions(ILogger<BlobFunctions> logger, IBlobStorageService blobService)
        {
            _logger = logger;
            _blobService = blobService;
        }

        [Function("UploadBlob")]
        public async Task<IActionResult> UploadBlob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "blob/upload")] HttpRequest req)
        {
            _logger.LogInformation("Uploading blob...");

            try
            {
                if (!req.HasFormContentType)
                    return new BadRequestObjectResult("Expected multipart/form-data");

                var form = await req.ReadFormAsync();
                var file = form.Files["file"];

                if (file == null || file.Length == 0)
                    return new BadRequestObjectResult("No file uploaded");

                // Use your actual method name and specify container
                var blobUrl = await _blobService.UploadFileAsync(file, "productimages"); // or "documents"

                return new OkObjectResult(new
                {
                    fileName = file.FileName,
                    blobUrl = blobUrl,
                    message = "File uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob.");
                return new StatusCodeResult(500);
            }
        }

        [Function("GetBlob")]
        public async Task<IActionResult> GetBlob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blob/{fileName}")] HttpRequest req,
            string fileName)
        {
            _logger.LogInformation($"Getting blob {fileName}...");

            try
            {
                // Use your actual method name and specify container
                var stream = await _blobService.DownloadFileAsync(fileName, "productimages"); // or "documents"

                if (stream == null)
                    return new NotFoundResult();

                return new FileStreamResult(stream, "application/octet-stream")
                {
                    FileDownloadName = fileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving blob {fileName}.");
                return new StatusCodeResult(500);
            }
        }

        [Function("DeleteBlob")]
        public async Task<IActionResult> DeleteBlob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "blob/{fileName}")] HttpRequest req,
            string fileName)
        {
            _logger.LogInformation($"Deleting blob {fileName}...");

            try
            {
                var success = await _blobService.DeleteFileAsync(fileName, "productimages"); // or "documents"
                return success ? new OkObjectResult(new { message = "File deleted successfully" })
                              : new NotFoundResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting blob {fileName}.");
                return new StatusCodeResult(500);
            }
        }
    }
}