namespace ABCRetailers.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<bool> UploadFileAsync(IFormFile file, string shareName, string directoryPath = "");
        Task<bool> DeleteFileAsync(string fileName, string shareName, string directoryPath = "");
        Task<Stream> DownloadFileAsync(string fileName, string shareName, string directoryPath = "");
        Task<List<string>> GetFileListAsync(string shareName, string directoryPath = "");
        Task<bool> FileExistsAsync(string fileName, string shareName, string directoryPath = "");
    }
}