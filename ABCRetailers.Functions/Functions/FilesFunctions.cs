using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCRetailers.Services.Interfaces;
using System.Threading.Tasks;
using System.IO;

namespace ABCRetailers.Functions
{
    public class FilesFunctions
    {
        private readonly IFileStorageService _fileService;
        private readonly ILogger<FilesFunctions> _logger;

        public FilesFunctions(ILogger<FilesFunctions> logger, IFileStorageService fileService)
        {
            _logger = logger;
            _fileService = fileService;
        }

        [Function("UploadFile")]
        public async Task<IActionResult> UploadFile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "files/upload")] HttpRequest req)
        {
            _logger.LogInformation("Uploading file...");

            try
            {
                if (!req.HasFormContentType)
                    return new BadRequestObjectResult("Expected multipart/form-data");

                var form = await req.ReadFormAsync();
                var file = form.Files["file"];

                if (file == null || file.Length == 0)
                    return new BadRequestObjectResult("No file uploaded");

                // Use your actual method name and parameters
                var success = await _fileService.UploadFileAsync(file, "documents", "");

                if (success)
                {
                    return new OkObjectResult(new
                    {
                        fileName = file.FileName,
                        message = "File uploaded successfully"
                    });
                }
                else
                {
                    return new StatusCodeResult(500);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file.");
                return new StatusCodeResult(500);
            }
        }

        [Function("DownloadFile")]
        public async Task<IActionResult> DownloadFile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "files/{fileName}")] HttpRequest req,
            string fileName)
        {
            _logger.LogInformation($"Downloading file {fileName}...");

            try
            {
                // Use your actual method name and parameters
                var stream = await _fileService.DownloadFileAsync(fileName, "documents", "");

                if (stream == null)
                    return new NotFoundResult();

                return new FileStreamResult(stream, "application/octet-stream")
                {
                    FileDownloadName = fileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file {fileName}.");
                return new StatusCodeResult(500);
            }
        }

        [Function("DeleteFile")]
        public async Task<IActionResult> DeleteFile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "files/{fileName}")] HttpRequest req,
            string fileName)
        {
            _logger.LogInformation($"Deleting file {fileName}...");

            try
            {
                var success = await _fileService.DeleteFileAsync(fileName, "documents", "");
                return success ? new OkObjectResult(new { message = "File deleted successfully" })
                              : new NotFoundResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileName}.");
                return new StatusCodeResult(500);
            }
        }

        [Function("GetFileList")]
        public async Task<IActionResult> GetFileList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "files")] HttpRequest req)
        {
            _logger.LogInformation("Getting file list...");

            try
            {
                var files = await _fileService.GetFileListAsync("documents", "");
                return new OkObjectResult(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file list.");
                return new StatusCodeResult(500);
            }
        }
    }
}