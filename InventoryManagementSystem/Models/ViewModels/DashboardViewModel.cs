using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Core Counts
        public int ProductCount { get; set; }
        public int SupplierCount { get; set; }
        public int CustomerCount { get; set; }
        public int LowStockCount { get; set; }
        public int HighStockCount { get; set; }

        // Financial KPIs
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal NetProfit { get; set; }
        public decimal PendingReceivables { get; set; }
        public decimal PendingPayables { get; set; }
        public decimal TotalStockValue { get; set; }

        // Chart Data Lists
        public List<string> MonthlyLabels { get; set; } = new();
        public List<decimal> MonthlySalesData { get; set; } = new();
        public List<decimal> MonthlyPurchasesData { get; set; } = new();

        public List<string> TopProductLabels { get; set; } = new();
        public List<int> TopProductQuantities { get; set; } = new();

        // Data Tables
        public List<Product> LowStockProducts { get; set; } = new();
        public List<Product> HighStockProducts { get; set; } = new();
        public List<Sale> RecentSales { get; set; } = new();
    }
}
