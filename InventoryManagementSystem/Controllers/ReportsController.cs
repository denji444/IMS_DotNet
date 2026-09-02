using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    }
}
