// Services/Interfaces/IQueueStorageService.cs
namespace ABCRetailers.Services.Interfaces
{
    public interface IQueueStorageService
    {
        Task<bool> SendMessageAsync(string message, string queueName);
        Task<string?> GetMessageAsync(string queueName);  // This allows null return
        Task<bool> DeleteMessageAsync(string messageId, string popReceipt, string queueName);
        Task<int> GetMessageCountAsync(string queueName);
    }
}