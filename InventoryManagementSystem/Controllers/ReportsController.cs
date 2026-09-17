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
    [Authorize(Policy = "Reports")]
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
            var products = await _context.Products.AsNoTracking().ToListAsync();

            // Aggregated Purchases by Product
            var multiPurc = await _context.PurchaseItems.AsNoTracking()
                .GroupBy(pi => pi.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity), Cost = g.Sum(x => x.TotalCost) })
                .ToListAsync();

            var legacyPurc = await _context.Purchases.AsNoTracking()
                .Where(pu => pu.ProductId.HasValue && !pu.Items.Any())
                .GroupBy(pu => pu.ProductId!.Value)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity), Cost = g.Sum(x => x.TotalCost) })
                .ToListAsync();

            var purcDict = multiPurc.Concat(legacyPurc)
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => new { Qty = g.Sum(x => x.Qty), Cost = g.Sum(x => x.Cost) });

            // Aggregated Sales by Product
            var multiSales = await _context.SaleItems.AsNoTracking()
                .GroupBy(si => si.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var legacySales = await _context.Sales.AsNoTracking()
                .Where(s => s.ProductId.HasValue && !s.Items.Any())
                .GroupBy(s => s.ProductId!.Value)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var salesDict = multiSales.Concat(legacySales)
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => new { Qty = g.Sum(x => x.Qty), Revenue = g.Sum(x => x.Revenue) });

            var reportData = products.Select(p =>
            {
                purcDict.TryGetValue(p.Id, out var purcInfo);
                salesDict.TryGetValue(p.Id, out var salesInfo);

                return new ProductReportViewModel
                {
                    ProductId = p.Id,
                    Sku = p.Sku,
                    ProductName = p.Name,
                    Variant = p.Variant,
                    AvailableQuantity = p.StockQuantity,
                    PurchasedQuantity = purcInfo?.Qty ?? 0,
                    PurchasedCost = purcInfo?.Cost ?? 0m,
                    SoldQuantity = salesInfo?.Qty ?? 0,
                    SalesRevenue = salesInfo?.Revenue ?? 0m
                };
            }).ToList();

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
            var saleItemsQuery = _context.SaleItems.AsQueryable();
            var purchaseItemsQuery = _context.PurchaseItems.AsQueryable();

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                salesQuery = salesQuery.Where(s => s.SaleDate >= start);
                purchasesQuery = purchasesQuery.Where(p => p.PurchaseDate >= start);
                saleItemsQuery = saleItemsQuery.Where(si => si.Sale != null && si.Sale.SaleDate >= start);
                purchaseItemsQuery = purchaseItemsQuery.Where(pi => pi.Purchase != null && pi.Purchase.PurchaseDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                salesQuery = salesQuery.Where(s => s.SaleDate <= end);
                purchasesQuery = purchasesQuery.Where(p => p.PurchaseDate <= end);
                saleItemsQuery = saleItemsQuery.Where(si => si.Sale != null && si.Sale.SaleDate <= end);
                purchaseItemsQuery = purchaseItemsQuery.Where(pi => pi.Purchase != null && pi.Purchase.PurchaseDate <= end);
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
                    Revenue = (saleItemsQuery.Where(si => si.ProductId == p.Id).Sum(si => (decimal?)si.TotalAmount) ?? 0m) +
                              (salesQuery.Where(s => s.ProductId == p.Id && !s.Items.Any()).Sum(s => (decimal?)s.TotalAmount) ?? 0m),
                    Cost = (purchaseItemsQuery.Where(pi => pi.ProductId == p.Id).Sum(pi => (decimal?)pi.TotalCost) ?? 0m) +
                           (purchasesQuery.Where(pu => pu.ProductId == p.Id && !pu.Items.Any()).Sum(pu => (decimal?)pu.TotalCost) ?? 0m)
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

        [HttpGet]
        public async Task<IActionResult> GetCustomersList()
        {
            var customerRoleId = await _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var customers = await _context.Users
                .Where(u => customerRoleId != null && _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == customerRoleId))
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.Cnic
                })
                .ToListAsync();

            if (!customers.Any())
            {
                customers = await _context.Sales
                    .Where(s => s.Customer != null)
                    .Select(s => s.Customer!)
                    .Distinct()
                    .OrderBy(u => u.FullName)
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.PhoneNumber,
                        u.Cnic
                    })
                    .ToListAsync();
            }

            return Json(customers);
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerTransactionReport(string? customerId, DateTime? startDate, DateTime? endDate)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                return Json(new
                {
                    customer = new { fullName = "Select a Customer", email = "", phone = "", cnic = "" },
                    summary = new { totalTransactions = 0, totalSalesAmount = 0m, totalPaidAmount = 0m, totalBalanceDue = 0m },
                    transactions = new object[] { }
                });
            }

            var customerUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == customerId);
            var customerInfo = new
            {
                fullName = customerUser?.FullName ?? "N/A",
                email = customerUser?.Email ?? "N/A",
                phone = customerUser?.PhoneNumber ?? "N/A",
                cnic = customerUser?.Cnic ?? "N/A"
            };

            var salesQuery = _context.Sales.AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .Include(s => s.Installments)
                .Where(s => s.CustomerId == customerId);

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                salesQuery = salesQuery.Where(s => s.SaleDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                salesQuery = salesQuery.Where(s => s.SaleDate <= end);
            }

            var salesList = await salesQuery
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var transactions = salesList.Select(s =>
            {
                string statusBadge = s.PaymentStatus == "Paid" ? "bg-success" : (s.PaymentStatus == "Partial" ? "bg-warning text-dark" : "bg-danger");
                string itemsSummary = s.Items.Any()
                    ? string.Join(", ", s.Items.Select(i => $"{(i.Product != null ? (string.IsNullOrEmpty(i.Product.Variant) ? i.Product.Name : $"{i.Product.Name} ({i.Product.Variant})") : "Item")} x{i.Quantity}"))
                    : (s.Product != null ? $"{(string.IsNullOrEmpty(s.Product.Variant) ? s.Product.Name : $"{s.Product.Name} ({s.Product.Variant})")} x{s.Quantity}" : "N/A");

                return new
                {
                    saleId = s.Id,
                    invoiceNo = s.InvoiceNo,
                    saleDate = s.SaleDate.ToString("yyyy-MM-dd HH:mm"),
                    itemsSummary = itemsSummary,
                    paymentMode = s.PaymentMode.ToString(),
                    paymentMethod = s.PaymentMethod.ToString(),
                    totalAmount = s.TotalAmount,
                    paidAmount = s.TotalPaidAmount,
                    balance = s.BalanceDue,
                    status = s.PaymentStatus,
                    statusBadge = statusBadge
                };
            }).ToList();

            var totalTransactions = transactions.Count;
            var totalSalesAmount = transactions.Sum(t => t.totalAmount);
            var totalPaidAmount = transactions.Sum(t => t.paidAmount);
            var totalBalanceDue = transactions.Sum(t => t.balance);

            return Json(new
            {
                customer = customerInfo,
                summary = new
                {
                    totalTransactions,
                    totalSalesAmount,
                    totalPaidAmount,
                    totalBalanceDue
                },
                transactions = transactions
            });
        }

        [HttpGet]
        public async Task<IActionResult> PrintCustomerStatement(string customerId, DateTime? startDate, DateTime? endDate)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                return NotFound("Customer ID is required.");
            }

            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == customerId);
            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            var salesQuery = _context.Sales
                .Include(s => s.Product)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .Include(s => s.Installments)
                .Where(s => s.CustomerId == customerId);

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                salesQuery = salesQuery.Where(s => s.SaleDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                salesQuery = salesQuery.Where(s => s.SaleDate <= end);
            }

            var sales = await salesQuery.OrderBy(s => s.SaleDate).ToListAsync();

            ViewData["Customer"] = customer;
            ViewData["StartDate"] = startDate;
            ViewData["EndDate"] = endDate;
            ViewData["CompanyProfile"] = await _context.CompanyProfiles.FirstOrDefaultAsync();

            return View(sales);
        }
    }
}
