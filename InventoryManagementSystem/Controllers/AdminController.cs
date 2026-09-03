using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Models.ViewModels;

namespace InventoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly InventoryDbContext _context;

        public AdminController(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();

            // Core Counts
            viewModel.ProductCount = await _context.Products.CountAsync();
            viewModel.SupplierCount = await _context.Suppliers.CountAsync();

            var customerRoleId = await _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            viewModel.CustomerCount = await _context.Users
                .CountAsync(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == customerRoleId));

            viewModel.LowStockCount = await _context.Products.CountAsync(p => p.StockQuantity <= 5);

            // Financial KPIs
            viewModel.TotalStockValue = await _context.Products.SumAsync(p => (decimal?)p.StockQuantity * p.Price) ?? 0m;
            viewModel.TotalSales = await _context.Sales.SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
            viewModel.TotalPurchases = await _context.Purchases.SumAsync(p => (decimal?)p.TotalCost) ?? 0m;
            viewModel.NetProfit = viewModel.TotalSales - viewModel.TotalPurchases;

            // Pending Receivables (Unpaid Sale Installments) & Payables (Unpaid Purchase Installments)
            var pendingSaleInst = await _context.SaleInstallments
                .Where(si => si.Status != "Paid")
                .SumAsync(si => (decimal?)(si.Amount - si.PaidAmount)) ?? 0m;

            viewModel.PendingReceivables = pendingSaleInst;

            var pendingPurcInst = await _context.PurchaseInstallments
                .Where(pi => pi.Status != "Paid")
                .SumAsync(pi => (decimal?)(pi.Amount - pi.PaidAmount)) ?? 0m;

            viewModel.PendingPayables = pendingPurcInst;

            // Past 6 Months Financial Trend Data
            var now = DateTime.UtcNow;
            var months = new List<string>();
            var monthlySales = new List<decimal>();
            var monthlyPurchases = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var year = monthDate.Year;
                var month = monthDate.Month;

                months.Add(monthDate.ToString("MMM yyyy"));

                var salesSum = await _context.Sales
                    .Where(s => s.SaleDate.Year == year && s.SaleDate.Month == month)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

                var purcSum = await _context.Purchases
                    .Where(p => p.PurchaseDate.Year == year && p.PurchaseDate.Month == month)
                    .SumAsync(p => (decimal?)p.TotalCost) ?? 0m;

                monthlySales.Add(salesSum);
                monthlyPurchases.Add(purcSum);
            }

            viewModel.MonthlyLabels = months;
            viewModel.MonthlySalesData = monthlySales;
            viewModel.MonthlyPurchasesData = monthlyPurchases;

            // Top 5 Selling Products
            var topProducts = await _context.Sales
                .GroupBy(s => s.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(s => s.Quantity)
                })
                .OrderByDescending(g => g.TotalQuantity)
                .Take(5)
                .ToListAsync();

            var topProductIds = topProducts.Select(tp => tp.ProductId).ToList();
            var productDetails = await _context.Products
                .Where(p => topProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => string.IsNullOrWhiteSpace(p.Variant) ? p.Name : $"{p.Name} ({p.Variant})");

            foreach (var tp in topProducts)
            {
                if (productDetails.TryGetValue(tp.ProductId, out var label))
                {
                    viewModel.TopProductLabels.Add(label);
                    viewModel.TopProductQuantities.Add(tp.TotalQuantity);
                }
            }

            // Low Stock Items List (Stock <= 5)
            viewModel.LowStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= 5)
                .OrderBy(p => p.StockQuantity)
                .Take(10)
                .ToListAsync();

            // Recent 5 Sales
            viewModel.RecentSales = await _context.Sales
                .Include(s => s.Product)
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync();

            return View(viewModel);
        }
    }
}
