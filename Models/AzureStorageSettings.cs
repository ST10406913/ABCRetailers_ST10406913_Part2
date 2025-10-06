namespace ABCRetailers.Models
{
    public class AzureStorageSettings
    {
        public required string ConnectionString { get; set; }
        public required TableNames TableNames { get; set; }
        public required BlobContainerNames BlobContainerNames { get; set; }
        public required QueueNames QueueNames { get; set; }
        public required FileShareNames FileShareNames { get; set; }
    }

    public class TableNames
    {
        public string Customers { get; set; } = "Customers";
        public string Products { get; set; } = "Products";
        public string Orders { get; set; } = "Orders";
    }

    public class BlobContainerNames
    {
        public string ProductImages { get; set; } = "product-images";
        public string Documents { get; set; } = "documents";
    }

    public class QueueNames
    {
        public string OrderQueue { get; set; } = "orders";
    }

    public class FileShareNames
    {
        public string Documents { get; set; } = "documents";
    }
}