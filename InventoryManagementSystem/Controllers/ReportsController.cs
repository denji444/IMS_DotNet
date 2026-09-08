using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models.ViewModels;

namespace InventoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly InventoryDbContext _context;

        public ReportsController(InventoryDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetReportData()
        {
            var reportData = await _context.Products
                .Select(p => new ProductReportViewModel
                {
                    ProductId = p.Id,
                    Sku = p.Sku,
                    ProductName = p.Name,
                    Variant = p.Variant,
                    AvailableQuantity = p.StockQuantity,
                    PurchasedQuantity = _context.Purchases.Where(pu => pu.ProductId == p.Id).Sum(pu => (int?)pu.Quantity) ?? 0,
                    PurchasedCost = _context.Purchases.Where(pu => pu.ProductId == p.Id).Sum(pu => (decimal?)pu.TotalCost) ?? 0,
                    SoldQuantity = _context.Sales.Where(s => s.ProductId == p.Id).Sum(s => (int?)s.Quantity) ?? 0,
                    SalesRevenue = _context.Sales.Where(s => s.ProductId == p.Id).Sum(s => (decimal?)s.TotalAmount) ?? 0
                })
                .ToListAsync();

            var totalProducts = reportData.Count;
            var totalAvailableQty = reportData.Sum(r => r.AvailableQuantity);
            var totalPurchasedCost = reportData.Sum(r => r.PurchasedCost);
            var totalSalesRevenue = reportData.Sum(r => r.SalesRevenue);
            var totalProfit = reportData.Sum(r => r.NetProfit);

            return Json(new
            {
                summary = new
                {
                    totalProducts,
                    totalAvailableQty,
                    totalPurchasedCost,
                    totalSalesRevenue,
                    totalProfit
                },
                data = reportData
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetProfitAndLossData(DateTime? startDate, DateTime? endDate)
        {
            var salesQuery = _context.Sales.AsQueryable();
            var purchasesQuery = _context.Purchases.AsQueryable();

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                salesQuery = salesQuery.Where(s => s.SaleDate >= start);
                purchasesQuery = purchasesQuery.Where(p => p.PurchaseDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                salesQuery = salesQuery.Where(s => s.SaleDate <= end);
                purchasesQuery = purchasesQuery.Where(p => p.PurchaseDate <= end);
            }

            var grossRevenue = await salesQuery.SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
            var cogs = await purchasesQuery.SumAsync(p => (decimal?)p.TotalCost) ?? 0m;
            var grossProfit = grossRevenue - cogs;

            // Monthly Payroll for Active Employees
            var monthlyPayroll = await _context.Employees
                .Where(e => e.Status == "Active")
                .SumAsync(e => (decimal?)e.Salary) ?? 0m;

            // Calculate months in date range or default to 1 month
            double totalMonths = 1;
            if (startDate.HasValue && endDate.HasValue && endDate.Value >= startDate.Value)
            {
                var days = (endDate.Value - startDate.Value).TotalDays;
                totalMonths = Math.Max(1.0, Math.Round(days / 30.0, 1));
            }
            var estimatedPayrollExpense = Math.Round(monthlyPayroll * (decimal)totalMonths, 2);

            var netOperatingIncome = grossProfit - estimatedPayrollExpense;

            // Itemized Breakdown by Product
            var itemizedBreakdown = await _context.Products
                .Select(p => new
                {
                    p.Id,
                    p.Sku,
                    ProductName = p.Name,
                    p.Variant,
                    Revenue = salesQuery.Where(s => s.ProductId == p.Id).Sum(s => (decimal?)s.TotalAmount) ?? 0m,
                    Cost = purchasesQuery.Where(pu => pu.ProductId == p.Id).Sum(pu => (decimal?)pu.TotalCost) ?? 0m
                })
                .Where(x => x.Revenue > 0 || x.Cost > 0)
                .ToListAsync();

            var itemizedList = itemizedBreakdown.Select(x => new
            {
                x.Sku,
                Name = string.IsNullOrEmpty(x.Variant) ? x.ProductName : $"{x.ProductName} ({x.Variant})",
                x.Revenue,
                x.Cost,
                GrossMargin = x.Revenue - x.Cost
            }).ToList();

            return Json(new
            {
                grossRevenue,
                cogs,
                grossProfit,
                monthlyPayroll,
                estimatedPayrollExpense,
                netOperatingIncome,
                items = itemizedList
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetInstallmentAgingData()
        {
            var today = DateTime.UtcNow.Date;

            // Unpaid Customer Sale Installments
            var saleInstallments = await _context.SaleInstallments
                .Include(si => si.Sale)
                    .ThenInclude(s => s!.Customer)
                .Include(si => si.Sale)
                    .ThenInclude(s => s!.Product)
                .Where(si => si.Status != "Paid")
                .OrderBy(si => si.DueDate)
                .ToListAsync();

            var customerAging = saleInstallments.Select(si =>
            {
                var remaining = si.Amount - si.PaidAmount;
                var daysOverdue = (today - si.DueDate.Date).Days;
                string bucket;
                string statusBadge;

                if (daysOverdue > 30)
                {
                    bucket = "Critical Overdue (31+ Days)";
                    statusBadge = "bg-danger text-white";
                }
                else if (daysOverdue > 0)
                {
                    bucket = "Overdue (1-30 Days)";
                    statusBadge = "bg-warning text-dark";
                }
                else if (daysOverdue == 0)
                {
                    bucket = "Due Today";
                    statusBadge = "bg-info text-white";
                }
                else
                {
                    bucket = "Upcoming";
                    statusBadge = "bg-secondary text-white";
                }

                return new
                {
                    si.Id,
                    si.SaleId,
                    InvoiceNo = si.Sale?.InvoiceNo ?? "N/A",
                    CustomerName = si.Sale?.Customer?.FullName ?? "N/A",
                    CustomerEmail = si.Sale?.Customer?.Email ?? "",
                    ProductName = si.Sale?.Product != null ? (string.IsNullOrEmpty(si.Sale.Product.Variant) ? si.Sale.Product.Name : $"{si.Sale.Product.Name} ({si.Sale.Product.Variant})") : "N/A",
                    si.InstallmentNumber,
                    si.Amount,
                    si.PaidAmount,
                    RemainingBalance = remaining,
                    DueDate = si.DueDate.ToString("yyyy-MM-dd"),
                    DaysOverdue = daysOverdue > 0 ? daysOverdue : 0,
                    AgingBucket = bucket,
                    StatusBadge = statusBadge
                };
            }).ToList();

            // Summary Totals
            var totalDueToday = customerAging.Where(x => x.DaysOverdue == 0).Sum(x => x.RemainingBalance);
            var totalOverdue1To30 = customerAging.Where(x => x.DaysOverdue >= 1 && x.DaysOverdue <= 30).Sum(x => x.RemainingBalance);
            var totalOverdue30Plus = customerAging.Where(x => x.DaysOverdue > 30).Sum(x => x.RemainingBalance);
            var totalReceivables = customerAging.Sum(x => x.RemainingBalance);

            return Json(new
            {
                summary = new
                {
                    totalReceivables,
                    totalDueToday,
                    totalOverdue1To30,
                    totalOverdue30Plus
                },
                installments = customerAging
            });
        }
    }
}
