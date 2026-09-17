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
    [Authorize(Policy = "Dashboard")]
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
            viewModel.ProductCount = await _context.Products.AsNoTracking().CountAsync();
            viewModel.SupplierCount = await _context.Suppliers.AsNoTracking().CountAsync();

            var customerRoleId = await _context.Roles.AsNoTracking()
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            viewModel.CustomerCount = await _context.Users.AsNoTracking()
                .CountAsync(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == customerRoleId));

            viewModel.LowStockCount = await _context.Products.AsNoTracking().CountAsync(p => p.StockQuantity <= 5);
            viewModel.HighStockCount = await _context.Products.AsNoTracking().CountAsync(p => p.StockQuantity >= 50);

            // Financial KPIs
            viewModel.TotalStockValue = await _context.Products.AsNoTracking().SumAsync(p => (decimal?)p.StockQuantity * (p.Price ?? 0m)) ?? 0m;
            viewModel.TotalSales = await _context.Sales.AsNoTracking().SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
            viewModel.TotalPurchases = await _context.Purchases.AsNoTracking().SumAsync(p => (decimal?)p.TotalCost) ?? 0m;
            viewModel.NetProfit = viewModel.TotalSales - viewModel.TotalPurchases;

            // Pending Receivables & Payables
            viewModel.PendingReceivables = await _context.SaleInstallments.AsNoTracking()
                .Where(si => si.Status != "Paid")
                .SumAsync(si => (decimal?)(si.Amount - si.PaidAmount)) ?? 0m;

            viewModel.PendingPayables = await _context.PurchaseInstallments.AsNoTracking()
                .Where(pi => pi.Status != "Paid")
                .SumAsync(pi => (decimal?)(pi.Amount - pi.PaidAmount)) ?? 0m;

            // Past 6 Months Financial Trend Data (Batched via 2 Grouped Queries instead of 12 sequential queries)
            var now = DateTime.UtcNow;
            var sixMonthsAgo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

            var monthlySalesGroup = await _context.Sales.AsNoTracking()
                .Where(s => s.SaleDate >= sixMonthsAgo)
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(s => s.TotalAmount) })
                .ToListAsync();

            var monthlyPurchasesGroup = await _context.Purchases.AsNoTracking()
                .Where(p => p.PurchaseDate >= sixMonthsAgo)
                .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.TotalCost) })
                .ToListAsync();

            var salesDict = monthlySalesGroup.ToDictionary(x => $"{x.Year}-{x.Month}", x => x.Total);
            var purcDict = monthlyPurchasesGroup.ToDictionary(x => $"{x.Year}-{x.Month}", x => x.Total);

            var months = new List<string>();
            var monthlySales = new List<decimal>();
            var monthlyPurchases = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var key = $"{monthDate.Year}-{monthDate.Month}";

                months.Add(monthDate.ToString("MMM yyyy"));
                monthlySales.Add(salesDict.TryGetValue(key, out var sTotal) ? sTotal : 0m);
                monthlyPurchases.Add(purcDict.TryGetValue(key, out var pTotal) ? pTotal : 0m);
            }

            viewModel.MonthlyLabels = months;
            viewModel.MonthlySalesData = monthlySales;
            viewModel.MonthlyPurchasesData = monthlyPurchases;

            // Top 5 Selling Products
            var multiItemCounts = await _context.SaleItems.AsNoTracking()
                .GroupBy(si => si.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(si => si.Quantity)
                })
                .ToListAsync();

            var legacyCounts = await _context.Sales.AsNoTracking()
                .Where(s => s.ProductId.HasValue && !s.Items.Any())
                .GroupBy(s => s.ProductId!.Value)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(s => s.Quantity)
                })
                .ToListAsync();

            var topProducts = multiItemCounts.Concat(legacyCounts)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(x => x.TotalQuantity)
                })
                .OrderByDescending(g => g.TotalQuantity)
                .Take(5)
                .ToList();

            var topProductIds = topProducts.Select(tp => tp.ProductId).ToList();
            var productDetails = await _context.Products.AsNoTracking()
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
            viewModel.LowStockProducts = await _context.Products.AsNoTracking()
                .Where(p => p.StockQuantity <= 5)
                .OrderBy(p => p.StockQuantity)
                .Take(10)
                .ToListAsync();

            // High Stock Items List (Stock >= 50) - Overbought
            viewModel.HighStockProducts = await _context.Products.AsNoTracking()
                .Where(p => p.StockQuantity >= 50)
                .OrderByDescending(p => p.StockQuantity)
                .Take(10)
                .ToListAsync();

            // Recent 5 Sales
            viewModel.RecentSales = await _context.Sales.AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync();

            return View(viewModel);
        }
    }
}
