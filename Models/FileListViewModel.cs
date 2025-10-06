using System.Collections.Generic;

namespace ABCRetailers.Models
{
    public class FileListViewModel
    {
        public List<string> BlobFiles { get; set; } = new List<string>();
        public List<string> FileShareFiles { get; set; } = new List<string>();
    }
}