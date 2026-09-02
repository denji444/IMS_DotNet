using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;

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
            ViewBag.ProductCount = await _context.Products.CountAsync();
            ViewBag.SupplierCount = await _context.Suppliers.CountAsync();
            
            var customerRoleId = await _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            ViewBag.CustomerCount = await _context.Users
                .CountAsync(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == customerRoleId));

            ViewBag.TotalStockValue = await _context.Products.SumAsync(p => p.StockQuantity * p.Price);
            ViewBag.TotalPurchases = await _context.Purchases.SumAsync(p => p.TotalCost);
            ViewBag.TotalSales = await _context.Sales.SumAsync(s => s.TotalAmount);

            // Fetch recent 5 sales for dashboard view
            var recentSales = await _context.Sales
                .Include(s => s.Product)
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync();

            return View(recentSales);
        }
    }
}
