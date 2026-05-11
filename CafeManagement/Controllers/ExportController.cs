using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagement.Data;

namespace CafeManagement.Controllers
{
    // ================================================================
    // EXPORT CONTROLLER — Xuất CSV báo cáo
    // ✅ FIX: Đổi DateTime.Today → DateTime.UtcNow.Date để nhất quán UTC
    // ================================================================

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public class ExportController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public ExportController(CafeDbContext db) => _db = db;

        /// <summary>Xuất CSV doanh thu theo ngày</summary>
        [HttpGet("revenue")]
        public async Task<IActionResult> ExportRevenueCsv(
            [FromQuery] int branchId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            // ✅ FIX: Dùng UtcNow thay vì DateTime.Today (local time)
            var fromDate = (from?.Date ?? DateTime.UtcNow.Date.AddDays(-30)).ToUniversalTime();
            var toDate   = (to?.Date   ?? DateTime.UtcNow.Date).AddDays(1).ToUniversalTime(); // exclusive upper bound

            var data = await _db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .Where(o =>
                    o.BranchId == branchId &&
                    o.OrderStatus.Name == "COMPLETED" &&
                    o.CreatedAt >= fromDate &&
                    o.CreatedAt <  toDate)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new {
                    o.Id,
                    Date        = o.CreatedAt.ToString("dd/MM/yyyy"),
                    Time        = o.CreatedAt.ToString("HH:mm"),
                    o.OrderType,
                    TableName   = o.Table != null ? o.Table.Name : "Take Away",
                    Customer    = o.Customer != null ? o.Customer.FullName : "Khách lẻ",
                    o.SubTotal,
                    o.DiscountAmount,
                    o.FinalAmount,
                    o.CostAmount,
                    Profit      = o.FinalAmount - o.CostAmount
                })
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Mã Đơn,Ngày,Giờ,Loại,Bàn,Khách hàng,Tạm tính,Giảm giá,Tổng thu,Giá vốn,Lợi nhuận");

            foreach (var row in data)
            {
                csv.AppendLine(
                    $"{row.Id},{row.Date},{row.Time},{row.OrderType}," +
                    $"\"{row.TableName}\",\"{row.Customer}\"," +
                    $"{row.SubTotal},{row.DiscountAmount},{row.FinalAmount},{row.CostAmount},{row.Profit}");
            }

            var bytes   = System.Text.Encoding.UTF8.GetPreamble()
                          .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
                          .ToArray();
            var fileName = $"revenue_{branchId}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        /// <summary>Xuất CSV top món bán chạy</summary>
        [HttpGet("top-items")]
        public async Task<IActionResult> ExportTopItemsCsv(
            [FromQuery] int branchId,
            [FromQuery] int days = 30)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var items = await _db.OrderItems
                .Include(oi => oi.Order).ThenInclude(o => o.OrderStatus)
                .Include(oi => oi.MenuItem).ThenInclude(m => m.Category)
                .Where(oi =>
                    oi.Order.BranchId == branchId &&
                    oi.Order.OrderStatus.Name == "COMPLETED" &&
                    oi.Order.CreatedAt >= since)
                .GroupBy(oi => new { oi.MenuItemId, ItemName = oi.MenuItem.Name, CategoryName = oi.MenuItem.Category.Name })
                .Select(g => new {
                    g.Key.ItemName,
                    g.Key.CategoryName,
                    TotalQty     = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.TotalQty)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Xếp hạng,Tên món,Danh mục,Số lượng,Doanh thu");
            var rank = 1;
            foreach (var row in items)
                csv.AppendLine($"{rank++},\"{row.ItemName}\",\"{row.CategoryName}\",{row.TotalQty},{row.TotalRevenue}");

            var bytes   = System.Text.Encoding.UTF8.GetPreamble()
                          .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
                          .ToArray();
            var fileName = $"top_items_{branchId}_{days}days.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
    }
}
