using ABCRetailers.Models;

namespace ABCRetailers.ViewModels
{
    public class HomeViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Orders> RecentOrders { get; set; } = new List<Orders>();
        public Dictionary<string, int> OrderStatusCounts { get; set; } = new Dictionary<string, int>();
        public List<Products> LowStockProducts { get; set; } = new List<Products>();
    }
}