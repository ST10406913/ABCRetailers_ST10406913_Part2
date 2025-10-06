namespace ABCRetailers.Services.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string containerName);
        Task<bool> DeleteFileAsync(string fileName, string containerName);
        Task<Stream> DownloadFileAsync(string fileName, string containerName);
        Task<List<string>> GetBlobListAsync(string containerName);
        Task<bool> FileExistsAsync(string fileName, string containerName);
    }
}